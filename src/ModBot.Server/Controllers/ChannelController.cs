using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers.Dto;
using ModBot.Server.Helpers;
using ModBot.Server.Providers.Twitch;
using Newtonsoft.Json;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using TwitchLib.Api.Helix.Models.Chat.Emotes.GetChannelEmotes;
using TwitchLib.Api.Helix.Models.Chat.Emotes.GetGlobalEmotes;

namespace ModBot.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/channels")]
public class ChannelController(AppDbContext dbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetModeratedChannels()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthorizedResponse("Unauthorized");
        
        List<ChannelDto> channels = await dbContext.Channels
            .Where(channel => channel.ModeratorId == currentUser.Id)
            .Include(channel => channel.Broadcaster)
            .ThenInclude(user => user.BroadcasterChatMessages)
            .ThenInclude(chatMessage => chatMessage.ReplyToMessage) 
            .ThenInclude(m => m.ReplyToMessage)
            .Include(channel => channel.Moderator)
            .ThenInclude(user => user.ModeratorChatMessages)
            .ThenInclude(chatMessage => chatMessage.ReplyToMessage) 
            .ThenInclude(m => m.ReplyToMessage)
            .OrderBy(channel => channel.Broadcaster.Username)
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
                .Where(channel => channel.ModeratorId == currentUser.Id && channel.Broadcaster.Username == broadcasterLogin)
                .Include(channel => channel.Broadcaster)
                .ThenInclude(user => user.BroadcasterChatMessages)
                .ThenInclude(chatMessage => chatMessage.ReplyToMessage) 
                .ThenInclude(m => m.ReplyToMessage)
                .Include(channel => channel.Moderator)
                .ThenInclude(user => user.ModeratorChatMessages)
                .ThenInclude(chatMessage => chatMessage.ReplyToMessage) 
                .ThenInclude(m => m.ReplyToMessage)
                .OrderBy(channel => channel.Broadcaster.Username)
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
    
    [HttpGet]
    [Route("{broadcasterId}/badges")]
    public async Task<IActionResult> GetChannelBadges(string broadcasterId)
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthorizedResponse("Unauthorized");
        
        Helix client = TwitchApiClient.GetHelixClient(currentUser);
        // Helix client = TwitchApiClient.GetHelixClient(TwitchBotAuth.BotUser);

        GetGlobalChatBadgesResponse globalBadges = await client.Chat.GetGlobalChatBadgesAsync();
        GetChannelChatBadgesResponse channelBadges = await client.Chat.GetChannelChatBadgesAsync(broadcasterId);

        Dictionary<string, BadgeEmoteSet> mergedBadges = globalBadges.EmoteSet
            .GroupBy(g => g.SetId)
            .ToDictionary(g => g.Key, g => g.First())
            .Concat(
                channelBadges.EmoteSet
                    .GroupBy(c => c.SetId)
                    .ToDictionary(g => g.Key, g => g.First())
            )
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
        
        return Ok(new
        {
            Message = "success",
            Data = mergedBadges.Values
        });
    }
    
    [HttpGet]
    [Route("{broadcasterId}/emotes")]
    public async Task<IActionResult> GetChannelEmotes(string broadcasterId)
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthorizedResponse("Unauthorized");
        
        Helix client = TwitchApiClient.GetHelixClient(currentUser);
        // Helix client = TwitchApiClient.GetHelixClient(TwitchBotAuth.BotUser);

        GetGlobalEmotesResponse globalEmotes = await client.Chat.GetGlobalEmotesAsync();
        GetChannelEmotesResponse channelEmotes = await client.Chat.GetChannelEmotesAsync(broadcasterId);
        
        // Convert global emotes to dictionary
        var globalEmoteDict = globalEmotes.GlobalEmotes
            .ToDictionary(
                e => e.Id,
                e => new
                {
                    e.Id,
                    e.Name,
                    e.Images,
                    e.Format,
                    e.Scale,
                    e.ThemeMode,
                    IsGlobal = true
                }
            );

        // Convert channel emotes and merge with global emotes
        var mergedEmotes = channelEmotes.ChannelEmotes
            .ToDictionary(
                e => e.Id,
                e => new
                {
                    e.Id,
                    e.Name,
                    e.Images,
                    e.Format,
                    e.Scale,
                    e.ThemeMode,
                    IsGlobal = false
                }
            )
            .Concat(globalEmoteDict)
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
        
        return Ok(new
        {
            Message = "success",
            Data = mergedEmotes.Values
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