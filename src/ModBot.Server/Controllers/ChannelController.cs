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

        try
        {
            Helix client = TwitchApiClient.GetHelixClient(currentUser);

            GetGlobalChatBadgesResponse globalBadges = await client.Chat.GetGlobalChatBadgesAsync();
            GetChannelChatBadgesResponse channelBadges = await client.Chat.GetChannelChatBadgesAsync(broadcasterId);

            Dictionary<string, Dictionary<string, BadgeVersion>> mergedBadges = new();

            if (globalBadges is not null)
            {
                foreach (BadgeEmoteSet? badge in globalBadges.EmoteSet)
                {
                    IOrderedEnumerable<BadgeVersion> version = badge.Versions.OrderByDescending(v => int.TryParse(v.Id, out int id) ? id : 0);
                    foreach (BadgeVersion v in version)
                    {
                        if (!mergedBadges.ContainsKey(badge.SetId))
                        {
                            mergedBadges.Add(badge.SetId,[]);
                        }
                        
                        if (!mergedBadges[badge.SetId].ContainsKey(v.Id))
                        {
                            mergedBadges[badge.SetId].Add(v.Id, v);
                        }
                    }
                }
            }

            if (channelBadges is not null)
            {
                foreach (BadgeEmoteSet? badge in channelBadges.EmoteSet)
                {
                    IOrderedEnumerable<BadgeVersion> version = badge.Versions.OrderByDescending(v => int.TryParse(v.Id, out int id) ? id : 0);
                    foreach (BadgeVersion v in version)
                    {
                        if (!mergedBadges.ContainsKey(badge.SetId))
                        {
                            mergedBadges.Add(badge.SetId,[]);
                        }
                        
                        if (!mergedBadges[badge.SetId].ContainsKey(v.Id))
                        {
                            mergedBadges[badge.SetId].Add(v.Id, v);
                        }
                        else
                        {
                            mergedBadges[badge.SetId][v.Id] = v;
                        }
                    }
                }
            }
            
            return Ok(new
            {
                Message = "success",
                Data = mergedBadges
            });
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("{broadcasterId}/emotes")]
    public async Task<IActionResult> GetChannelEmotes(string broadcasterId)
    {
        try
        {
            User? currentUser = User.User();
            if (currentUser == null) return UnauthorizedResponse("Unauthorized");

            Helix client = TwitchApiClient.GetHelixClient(currentUser);
            
            GetGlobalEmotesResponse globalEmotes = await client.Chat.GetGlobalEmotesAsync();
            GetChannelEmotesResponse channelEmotes = await client.Chat.GetChannelEmotesAsync(broadcasterId);

            Dictionary<string, object> allEmotes = new();

            // Add global emotes
            foreach (GlobalEmote? emote in globalEmotes.GlobalEmotes)
            {
                allEmotes[emote.Id] = new
                {
                    emote.Id,
                    emote.Name,
                    emote.Images,
                    emote.Format,
                    emote.Scale,
                    emote.ThemeMode,
                    IsGlobal = true
                };
            }

            // Add or update channel emotes
            foreach (ChannelEmote? emote in channelEmotes.ChannelEmotes)
            {
                allEmotes[emote.Id] = new
                {
                    emote.Id,
                    emote.Name,
                    emote.Images,
                    emote.Format,
                    emote.Scale,
                    emote.ThemeMode,
                    IsGlobal = false
                };
            }

            return Ok(new
            {
                Message = "success",
                Data = allEmotes.Values
            });
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error fetching emotes for channel {BroadcasterId}", broadcasterId);
            return InternalServerErrorResponse("Failed to fetch emotes");
        }
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

public class BadgeEmoteSetDto
{
    public string SetId { get; set; } = string.Empty;
    public BadgeVersion[] Versions { get; set; } = [];
    public bool IsGlobalBadge { get; set; }
}