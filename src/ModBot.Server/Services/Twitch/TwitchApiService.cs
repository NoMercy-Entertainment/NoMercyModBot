// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using ModBot.Server.Providers.Twitch;
using NodaTime.TimeZones;
using RestSharp;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;

namespace ModBot.Server.Services.Twitch;

public class TwitchApiService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<TwitchApiService> _logger;

    public TwitchApiService(IConfiguration conf, ILogger<TwitchApiService> logger)
    {
        _conf = conf;
        _logger = logger;
    }
        
    public async Task<UserInfo?> GetUser(string accessToken, string? userId = null)
    {
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new("users");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);
        
        if(userId != null) request.AddQueryParameter("id", userId);

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) throw new("Failed to fetch user information.");

        UserInfoResponse? userInfoResponse = response.Content?.FromJson<UserInfoResponse>();
        if (userInfoResponse is null) throw new("Failed to parse user information.");
        
        return userInfoResponse.Data.FirstOrDefault();
    }
    
    public Task<UserInfo?> BotGetUser(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) throw new("No user id provided.");
        
        return GetUser(TwitchBotAuth.BotUser.AccessToken!, userId);

    }
    
    public async Task<List<UserInfo>?> GetUsers(string accessToken, string[] userIds)
    {        
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        if (userIds.Any(string.IsNullOrEmpty)) throw new("Invalid user id provided.");
        if (userIds.Length == 0) throw new($"userIds must contain at least 1 userId");
        if (userIds.Length > 100) throw new("Too many user ids provided.");
        
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new("users");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);
        
        foreach (string id in userIds)
        {
            request.AddQueryParameter("user_id", id);
        }
        
        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user information.");

        UserInfoResponse? userInfoResponse = response.Content?.FromJson<UserInfoResponse>();
        if (userInfoResponse is null) throw new("Failed to parse user information.");
        
        return userInfoResponse.Data;
    }
    
    public Task<List<UserInfo>?> BotGetUsers(string[] userIds)
    {
        if (userIds.Any(string.IsNullOrEmpty)) throw new("Invalid user id provided.");
        if (userIds.Length == 0) throw new($"userIds must contain at least 1 userId");
        if (userIds.Length > 100) throw new("Too many user ids provided.");
        
        return GetUsers(TwitchBotAuth.BotUser.AccessToken!, userIds);
    }
    
    public async Task<User> FetchUser(TokenResponse tokenResponse, string? countryCode = null, string? id = null)
    {
        UserInfo? userInfo = await GetUser(tokenResponse.AccessToken, id);
        if (userInfo is null) throw new("Failed to fetch user information.");
        
        IEnumerable<string>? zoneIds = TzdbDateTimeZoneSource.Default.ZoneLocations?
            .Where(x => x.CountryCode == countryCode)
            .Select(x => x.ZoneId)
            .ToList();

        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            DisplayName = userInfo.DisplayName,
            Description = userInfo.Description,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            OfflineImageUrl = userInfo.OfflineImageUrl,
            BroadcasterType = userInfo.BroadcasterType,
            Timezone = id == Globals.TwitchBotId ? "UTC" : zoneIds?.FirstOrDefault(),
            AccessToken = id is null || id == Globals.TwitchBotId ? tokenResponse.AccessToken : null,
            RefreshToken = id is null || id == Globals.TwitchBotId ? tokenResponse.RefreshToken : null,
            TokenExpiry = id is null || id == Globals.TwitchBotId ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn) : null
        };
        
        GetUserChatColorResponse? colors = await BotGetUserChatColors([userInfo.Id]);
        
        string? color = colors?.Data.First().Color;
        
        user.Color = string.IsNullOrEmpty(color)
            ? "#9146FF"
            : color;

        AppDbContext dbContext = new();
        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                OfflineImageUrl = newUser.OfflineImageUrl,
                Color = newUser.Color,
                BroadcasterType = newUser.BroadcasterType,
                AccessToken = newUser.AccessToken,
                RefreshToken = newUser.RefreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();
        
        return user;
    }
    
    public async Task<GetUserChatColorResponse?> BotGetUserChatColors(string[] userIds)
    {
        if (userIds.Any(string.IsNullOrEmpty)) throw new("Invalid user id provided.");
        if (userIds.Length == 0) throw new($"userIds must contain at least 1 userId");
        if (userIds.Length > 100) throw new("Too many user ids provided.");
        
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new($"chat/color");
        request.AddHeader("Authorization", $"Bearer {TwitchBotAuth.BotUser.AccessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);

        foreach (string id in userIds)
        {
            request.AddQueryParameter("user_id", id);
        }

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user color.");
        
        GetUserChatColorResponse? colors = response.Content?.FromJson<GetUserChatColorResponse>();
        if (colors is null) throw new("Failed to parse user chat color.");
        
        return colors;
    }
    
    public async Task<ChannelResponse> GetUserModeration(string accessToken, string userId)
    {
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        if (string.IsNullOrEmpty(userId)) throw new("No user id provided.");
        
        RestClient client = new(Globals.TwitchApiUrl);
        
        RestRequest request = new("moderation/channels");
                    request.AddHeader("Authorization", $"Bearer {accessToken}");
                    request.AddHeader("client-id", Globals.TwitchClientId);
                    request.AddParameter("user_id", userId);

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user information.");
        
        ChannelResponse? channelResponse = response.Content.FromJson<ChannelResponse>();
        if (channelResponse == null) throw new("Invalid response from Twitch.");
        
        return channelResponse;
    }
    
}