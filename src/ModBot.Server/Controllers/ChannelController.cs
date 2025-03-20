using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers.Dto;
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
        if (currentUser == null) return UnauthorizedResponse("Unauthorized");

        List<ChannelDto> channels = await dbContext.Channels
            .Where(channel => channel.ModeratorId == currentUser.Id)
            .Include(channel => channel.Moderator)
            .Include(channel => channel.Broadcaster)
            .OrderBy(channel => channel.BroadcasterLogin)
            .Select(channel => new ChannelDto(channel))
            .ToListAsync();

        return Ok(new
        {
            Message = "success",
            Data = channels
        });
    }
    
    [HttpGet]
    [Route("{broadcasterLogin}")]
    public async Task<IActionResult> GetChannel(string broadcasterLogin)
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthorizedResponse("Unauthorized");

        ChannelDto? channel = await dbContext.Channels
            .Where(channel => channel.ModeratorId == currentUser.Id && channel.BroadcasterLogin == broadcasterLogin)
            .Include(channel => channel.Moderator)
            .Include(channel => channel.Broadcaster)
            .Select(channel => new ChannelDto(channel))
            .FirstOrDefaultAsync();

        if (channel == null) 
            return NotFoundResponse("Channel not found");

        return Ok(new
        {
            Message = "success",
            Data = channel
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