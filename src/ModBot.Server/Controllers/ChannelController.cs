using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;
using Newtonsoft.Json;

namespace ModBot.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/moderation/channels")]
public class ChannelController(AppDbContext dbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetModeratedChannels()
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized();

        List<Channel> channels = await dbContext.Channels
            .Where(channel => channel.ModeratorId == currentUser.Id)
            .ToListAsync();

        return Ok(new
        {
            Message = "success",
            Data = channels
        });
    }
}

public class ChannelResponse
{
    [JsonProperty("data")] public List<ChannelData> Data { get; set; } = [];
}

public class ChannelData
{
    [JsonProperty("broadcaster_id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("broadcaster_login")] public string BroadCasterLogin { get; set; } = string.Empty;
    [JsonProperty("broadcaster_name")] public string BroadcasterName { get; set; } = string.Empty;
}