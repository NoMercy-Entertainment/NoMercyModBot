using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using RestSharp;
using TwitchLib.Api;
using TwitchLib.Api.Helix;

namespace ModBot.Server.Providers.Twitch;

public class TwitchApiClient
{
    private static readonly Dictionary<string, TwitchAPI> Clients = [];

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

        Clients.Add(user.Id, client);
    }

    public static Helix GetHelixClient(User user)
    {
        return Clients.First(twitchApi => twitchApi.Key == user.Id).Value.Helix;
    }

    public void RemoveClient(User user)
    {
        Clients.Remove(user.Id);
    }

    public void UpdateClient(User user)
    {
        TwitchAPI client = Clients.First(client => client.Key == user.Id).Value;
        client.Settings.AccessToken = user.AccessToken;
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

    public static async Task<UserInfo?> FetchUserInfo(string accessToken)
    {
        RestClient client = new(Globals.TwitchApiUrl);
        RestRequest request = new("users");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);

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
                _accessToken = newUser._accessToken,
                _refreshToken = newUser._refreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();

        return refreshToken;
    }
}