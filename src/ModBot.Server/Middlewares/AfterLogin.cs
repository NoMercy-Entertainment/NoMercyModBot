using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using ModBot.Server.Providers.Twitch;
using RestSharp;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Moderation.BlockedTerms;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using BlockedTerm = ModBot.Database.Models.BlockedTerm;
using User = ModBot.Database.Models.User;

namespace ModBot.Server.Middlewares;

public class AfterLogin
{
    public async Task Invoke(User user)
    {
        await GetModeratorChannels(user);
        await GetBlockedTerms(user);
    }

    private static async Task GetModeratorChannels(User user)
    {
        RestResponse response = await ApiClient.GetUserModeration(user);
        if (!response.IsSuccessful) throw new($"Failed to fetch moderated channels. {response.Content}");
        if (response.Content is null) throw new("No response from Twitch.");

        ChannelResponse? broadcasterResponse = response.Content.FromJson<ChannelResponse>();
        if (broadcasterResponse == null) throw new("Invalid response from Twitch.");

        List<Channel> channels = broadcasterResponse.Data
            .Select(c => new Channel
            {
                BroadcasterId = c.Id,
                ModeratorId = user.Id,
                BroadcasterLogin = c.BroadCasterLogin,
                BroadcasterName = c.BroadcasterName
            })
            .ToList();

        ApiClient api = new(user);
        Helix client = api.GetHelixClient(user);

        GetUsersResponse? users = await client.Users
            .GetUsersAsync(channels.Select(c => c.BroadcasterId).Distinct().ToList());

        List<User> mods = [];
        foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User newUser in users.Users)
        {
            User mod = new()
            {
                Id = newUser.Id,
                Username = newUser.Login,
                ProfileImageUrl = newUser.ProfileImageUrl
            };
            mods.Add(mod);
        }

        await using AppDbContext dbContext = new();

        await dbContext.Users.UpsertRange(mods)
            .On(u => new { u.Id })
            .WhenMatched((existing, incoming) => new()
            {
                Id = incoming.Id,
                Username = incoming.Username,
                ProfileImageUrl = incoming.ProfileImageUrl
            })
            .RunAsync();

        await dbContext.Channels.UpsertRange(channels)
            .On(c => new { c.ModeratorId, c.BroadcasterId })
            .WhenMatched((existing, incoming) => new()
            {
                BroadcasterId = existing.BroadcasterId,
                ModeratorId = existing.ModeratorId,
                BroadcasterLogin = incoming.BroadcasterLogin,
                BroadcasterName = incoming.BroadcasterName
            })
            .RunAsync();
    }

    private static async Task GetBlockedTerms(User user)
    {
        ApiClient api = new(user);
        Helix client = api.GetHelixClient(user);

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
                    term.ModeratorId = null;
                    continue;
                }

                if (term.ModeratorId is null || fetchModerators.Contains(term.ModeratorId)) continue;
                fetchModerators.Add(term.ModeratorId);
            }

            blockedTerms.AddRange(t);
        }

        GetUsersResponse? x = await client.Users
            .GetUsersAsync(fetchModerators.ToList());

        List<User> mods = [];
        foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User newUser in x.Users)
        {
            User mod = new()
            {
                Id = newUser.Id,
                Username = newUser.Login,
                ProfileImageUrl = newUser.ProfileImageUrl
            };
            mods.Add(mod);
        }

        await dbContext.Users.UpsertRange(mods)
            .On(u => new { u.Id })
            .WhenMatched((existing, incoming) => new()
            {
                Id = incoming.Id,
                Username = incoming.Username,
                ProfileImageUrl = incoming.ProfileImageUrl
            })
            .RunAsync();

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