using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using ModBot.Server.Providers.Twitch;
using RestSharp;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;
using TwitchLib.Api.Helix.Models.Moderation.BlockedTerms;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using BlockedTerm = ModBot.Database.Models.BlockedTerm;
using User = ModBot.Database.Models.User;

namespace ModBot.Server.Middlewares;

public class AfterLogin
{
    public async Task Invoke(User user)
    {
        _ = new TwitchApiClient(user);
        
        await GetModeratorChannels(user);
        await GetBlockedTerms(user);
        
        // Join your own channel
        _ = new TwitchLibClient(user: user, broadcaster: user);
                
        foreach (Channel channel in user.ModeratorChannels)
        {
            // Join the broadcaster's channel
            _ = new TwitchLibClient(user: user, broadcaster: channel.Broadcaster);
        }
    }

    private static async Task GetModeratorChannels(User user)
    {
        RestResponse response = await TwitchApiClient.GetUserModeration(user);
        if (!response.IsSuccessful) throw new($"Failed to fetch moderated channels. {response.Content}");
        if (response.Content is null) throw new("No response from Twitch.");

        ChannelResponse? broadcasterResponse = response.Content.FromJson<ChannelResponse>();
        if (broadcasterResponse == null) throw new("Invalid response from Twitch.");

        List<Channel> channels = broadcasterResponse.Data
            .Select(c => new Channel
            {
                BroadcasterId = c.Id,
                ModeratorId = user.Id,
            })
            .ToList();
        
        channels.Add(new()
        {
            BroadcasterId = user.Id,
            ModeratorId = user.Id
        });

        Helix client = TwitchApiClient.GetHelixClient(user);
        
        List<User> mods = await GetUsers(client, channels.Select(c => c.BroadcasterId).Distinct().ToList());
        await StoreUsers(mods);
        await StoreChannels(channels);

    }

    private static async Task GetBlockedTerms(User user)
    {
        Helix client = TwitchApiClient.GetHelixClient(user);

        List<BlockedTerm> blockedTerms = [];

        await using AppDbContext dbContext = new();
        List<Channel> channels = await dbContext.Channels
            .Where(channel => channel.ModeratorId == user.Id)
            .ToListAsync();

        List<User> users = await dbContext.Users
            .ToListAsync();

        List<string> fetchModerators = [];

        foreach (Channel channel in channels)
        {
            GetBlockedTermsResponse? terms = await client.Moderation
                .GetBlockedTermsAsync(
                    channel.BroadcasterId,
                    user.Id,
                    first: 100);

            List<BlockedTerm> t = terms.Data.Select(term => new BlockedTerm
            {
                Id = term.Id,
                BroadcasterId = term.BroadcasterId,
                Text = term.Text,
                ModeratorId = term.ModeratorId,
                ExpiresAt = term.ExpiresAt,
                CreatedAt = term.CreatedAt,
                UpdatedAt = term.UpdatedAt
            }).ToList();

            foreach (BlockedTerm term in t.Where(term => users.All(mod => mod.Id != term.ModeratorId)))
            {
                if (term.ModeratorId is "")
                {
                    term.ModeratorId = term.BroadcasterId;
                    continue;
                }

                if (fetchModerators.Contains(term.ModeratorId)) continue;
                fetchModerators.Add(term.ModeratorId);
            }

            blockedTerms.AddRange(t);
        }

        if (fetchModerators.Count > 0)
        {
            List<User> mods = await GetUsers(client, fetchModerators);
            await StoreUsers(mods);
        }

        await StoreBlockedTerms(blockedTerms);
    }

    private static async Task<List<User>> GetUsers(Helix client, List<string> ids)
    {
        GetUsersResponse? usersResponse = await client.Users
            .GetUsersAsync(ids);
        
        GetUserChatColorResponse colors = await client.Chat
            .GetUserChatColorAsync(ids);

        List<User> mods = [];
        foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User newUser in usersResponse.Users)
        {
            UserChatColorResponseModel? color = colors.Data.FirstOrDefault(c => c.UserId == newUser.Id);
            User mod = new()
            {
                Id = newUser.Id,
                Username = newUser.Login,
                DisplayName = newUser.DisplayName,
                Description = newUser.Description,
                ProfileImageUrl = newUser.ProfileImageUrl,
                OfflineImageUrl = newUser.OfflineImageUrl,
                Color = string.IsNullOrEmpty(color?.Color) 
                    ? "#9146FF" 
                    : color.Color,
                BroadcasterType = newUser.BroadcasterType,
            };
            mods.Add(mod);
        }

        return mods;
    }
    
    private static async Task StoreUsers(List<User> users)
    {
        await using AppDbContext dbContext = new();
        await dbContext.Users.UpsertRange(users)
            .On(u => new { u.Id })
            .WhenMatched((existing, incoming) => new()
            {
                Id = incoming.Id,
                Username = incoming.Username,
                DisplayName = incoming.DisplayName,
                Description = incoming.Description,
                ProfileImageUrl = incoming.ProfileImageUrl,
                OfflineImageUrl = incoming.OfflineImageUrl,
                Color = incoming.Color,
                BroadcasterType = incoming.BroadcasterType,
            })
            .RunAsync();
    }
    
    private static async Task StoreChannels(List<Channel> channels)
    {
        await using AppDbContext dbContext = new();
        await dbContext.Channels.UpsertRange(channels)
            .On(c => new { c.BroadcasterId, c.ModeratorId })
            .WhenMatched((existing, incoming) => new()
            {
                BroadcasterId = incoming.BroadcasterId,
                ModeratorId = incoming.ModeratorId,
            })
            .RunAsync();
    }
    
    private static async Task StoreBlockedTerms(List<BlockedTerm> blockedTerms)
    {
        await using AppDbContext dbContext= new();
        await dbContext.BlockedTerms.UpsertRange(blockedTerms)
            .On(b => new { b.Id })
            .WhenMatched((existing, incoming) => new()
            {
                Id = incoming.Id,
                BroadcasterId = incoming.BroadcasterId,
                Text = incoming.Text,
                ModeratorId = incoming.ModeratorId,
                ExpiresAt = incoming.ExpiresAt,
                UpdatedAt = incoming.UpdatedAt
            })
            .RunAsync();
    }
}