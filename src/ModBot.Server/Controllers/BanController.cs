using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;
using Newtonsoft.Json;

namespace ModBot.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/moderation/bans")]
public class BanController(AppDbContext dbContext) : BaseController
{
    [HttpGet]
    [Route("/api/moderation/banned")]
    public async Task<IActionResult> GetBans([FromBody] BannedRequest request)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        return Ok(request);
    }

    [HttpPost]
    [Route("/api/moderation/bans")]
    public async Task<IActionResult> BanUser([FromBody] BanRequest request)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        return Ok(request);
    }

    [HttpDelete]
    [Route("/api/moderation/bans")]
    public async Task<IActionResult> UnbanUser([FromBody] BanRequest request)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        return Ok(request);
    }
}

public record BannedRequest
{
    [JsonProperty("broadcaster_id")] public int BroadcasterId { get; set; }
    [JsonProperty("user_id")] public int? UserId { get; set; }
    [JsonProperty("first")] public int? First { get; set; } = 100;
    [JsonProperty("after")] public string? After { get; set; }
    [JsonProperty("before")] public string? Before { get; set; }
}

public record BanRequest
{
    [JsonProperty("user_id")] public int UserId { get; set; }
    [JsonProperty("duration")] public int Duration { get; set; }
    [JsonProperty("reason")] public string Reason { get; set; } = string.Empty;
}

public record UnbanRequest
{
    [JsonProperty("user_id")] public int UserId { get; set; }
}