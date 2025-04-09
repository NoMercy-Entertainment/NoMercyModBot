using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using NodaTime.TimeZones;
using RestSharp;
using TwitchLib.Api;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;

namespace ModBot.Server.Providers.Twitch;

public class TwitchApiClient
{
    private static readonly ConcurrentDictionary<string, TwitchAPI> Clients = [];

    public TwitchApiClient(User user)
    {
        if (Clients.Any(client => client.Key == user.Id))
        {
            Clients.First(twitchApi => twitchApi.Key == user.Id).Value.Settings.AccessToken = user.AccessToken;
            return;
        }

        TwitchAPI client = new()
        {
            Settings =
            {
                ClientId = Globals.TwitchClientId,
                Secret = Globals.ClientSecret,
                AccessToken = user.AccessToken,
                Scopes = Globals.TwitchScopes
            }
        };
        
        Console.WriteLine($"Initializing Twitch API client for {user.Username}");

        Clients.TryAdd(user.Id, client);
    }

    public static Helix GetHelixClient(User user)
    {
        KeyValuePair<string, TwitchAPI> helix = Clients.FirstOrDefault(twitchApi => twitchApi.Key == user.Id);
        if (helix.Key != null) return helix.Value.Helix;
        
        _ = new TwitchApiClient(user);
        return Clients.FirstOrDefault(twitchApi => twitchApi.Key == user.Id).Value.Helix;
    }

    public void RemoveClient(User user)
    {
        Clients.TryRemove(user.Id, out _);
    }

    public void UpdateClient(User user)
    {
        TwitchAPI client = Clients.First(client => client.Key == user.Id).Value;
        client.Settings.AccessToken = user.AccessToken;
    }
    
    public void UpdateToken(string newToken)
    {
        // Update the token in your API client
    }

    public static async Task<RestResponse> GetUserModeration(User user)
    {
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new("moderation/channels");
        request.AddHeader("Authorization", $"Bearer {user.AccessToken}");
        request.AddHeader("client-id", Globals.TwitchClientId);
        request.AddParameter("user_id", user.Id);

        return await client.ExecuteAsync(request);
    }

    public static async Task<UserInfo?> FetchUserInfo(string accessToken, string? id = null)
    {
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new("users");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);
        if(id != null)
        {
            request.AddQueryParameter("id", id);
        }

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) return null;

        UserInfoResponse? userInfoResponse = response.Content?.FromJson<UserInfoResponse>();
        return userInfoResponse?.Data.FirstOrDefault();
    }

    // local refresh token function
    public static async Task<TokenResponse?> RefreshToken(User user)
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        RestRequest request = new("token", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("refresh_token", user.RefreshToken);
        request.AddParameter("grant_type", "refresh_token");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) return null;

        TokenResponse? refreshToken = response.Content?.FromJson<TokenResponse>();

        if (refreshToken == null) return null;

        User updateUser = new()
        {
            Id = user.Id,
            AccessToken = refreshToken.AccessToken,
            RefreshToken = refreshToken.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(refreshToken.ExpiresIn)
        };

        AppDbContext dbContext = new();
        await dbContext.Users.Upsert(updateUser)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                AccessToken = newUser.AccessToken,
                RefreshToken = newUser.RefreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();

        return refreshToken;
    }
    
    internal static async Task<TokenResponse> BotToken()
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        RestRequest request = new("token", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("scope", string.Join(' ', Globals.Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) throw new("Failed to fetch bot token.");

        TokenResponse? botToken = response.Content?.FromJson<TokenResponse>();
        if (botToken is null) throw new("Failed to parse bot token.");

        return botToken;
    }

    public static async Task<User> FetchUser(TokenResponse tokenResponse, string? countryCode = null, string? id = null)
    {
        UserInfo? userInfo = await FetchUserInfo(tokenResponse.AccessToken, id);
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
        
        Helix client = GetHelixClient(user);
        if (client == null) throw new("Helix client is not initialized.");
        
        GetUserChatColorResponse colors = await client.Chat.GetUserChatColorAsync([userInfo.Id]);
        
        string color = colors.Data.First().Color;
        
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
}