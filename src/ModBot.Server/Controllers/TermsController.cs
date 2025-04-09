using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;
using ModBot.Server.Providers.Twitch;
using Newtonsoft.Json;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Moderation.BlockedTerms;
using BlockedTerm = ModBot.Database.Models.BlockedTerm;

namespace ModBot.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/moderation/terms")]
public class TermsController(AppDbContext dbContext) : BaseController
{
    [HttpGet]
    [Route("/api/moderation/blocked_terms")]
    [Route("/api/moderation/blocked_terms/{broadcasterId}")]
    public async Task<IActionResult> GetBans(string? broadcasterId = "")
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        List<BlockedTerm> blockedTerms = await dbContext.BlockedTerms
            .AsNoTracking()
            .Where(term => term.ModeratorId == currentUser.Id)
            .Where(term => broadcasterId!.Length <= 0 || term.BroadcasterId == broadcasterId)
            .Include(term => term.Moderator)
            .Include(term => term.Broadcaster)
            .ToListAsync();

        return Ok(blockedTerms);
    }

    [HttpPost]
    [Route("/api/moderation/blocked_terms")]
    public async Task<IActionResult> BanUser([FromBody] TermRequest request)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        Helix client = TwitchApiClient.GetHelixClient(currentUser);

        AddBlockedTermResponse terms = await client.Moderation
            .AddBlockedTermAsync(
                request.BroadcasterId,
                currentUser.Id,
                request.Term);

        return Ok(terms);
    }

    [HttpDelete]
    [Route("/api/moderation/blocked_terms")]
    public async Task<IActionResult> UnbanUser([FromBody] DeleteTermRequest request)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        Helix client = TwitchApiClient.GetHelixClient(currentUser);

        await client.Moderation.DeleteBlockedTermAsync(
            request.BroadcasterId,
            currentUser.Id,
            request.Id);

        return Ok(request);
    }
}

public record TermsRequest
{
    [JsonProperty("broadcaster_id")] public string? BroadcasterId { get; set; } = string.Empty;
    [JsonProperty("first")] public int? First { get; set; } = 100;
    [JsonProperty("after")] public string? After { get; set; }
}

public record TermRequest
{
    [JsonProperty("broadcaster_id")] public string BroadcasterId { get; set; } = string.Empty;
    [JsonProperty("term")] public string Term { get; set; } = string.Empty;
}

public record DeleteTermRequest
{
    [JsonProperty("broadcaster_id")] public string BroadcasterId { get; set; } = string.Empty;
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
}