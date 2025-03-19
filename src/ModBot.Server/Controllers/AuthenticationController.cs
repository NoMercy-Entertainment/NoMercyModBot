using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Helpers;
using ModBot.Server.Middlewares;
using ModBot.Server.Providers.Twitch;
using RestSharp;
using Newtonsoft.Json;

namespace ModBot.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(AppDbContext dbContext) : BaseController
{
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequestResponse(detail: error);
        }

        RestClient client = new($"{Globals.TwitchAuthUrl}/token");
        RestRequest request = new("", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("code", code);
        request.AddParameter("scope", string.Join(' ', Globals.Scopes));
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("redirect_uri", Globals.RedirectUri);

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
            return BadRequestResponse(detail: response.Content ?? "Failed to fetch token from Twitch.");
        if (response.Content is null) return BadRequestResponse(detail: "Invalid response from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) return BadRequestResponse(detail: response.Content);

        UserInfo? userInfo = await ApiClient.FetchUserInfo(tokenResponse.AccessToken);
        if (userInfo == null) return BadRequestResponse(detail: "Failed to fetch user information.");

        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            DisplayName = userInfo.DisplayName,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };

        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                _accessToken = newUser._accessToken,
                _refreshToken = newUser._refreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();

        AfterLogin afterLogin = new();
        await afterLogin.Invoke(user);

        return Ok(new
        {
            Message = "Moderator logged in successfully",
            User = new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.TokenExpiry,
                user.ProfileImageUrl,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken
            }
        });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateSession()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthenticatedResponse("User not logged in.");
    
        try {
            RestClient client = new($"{Globals.TwitchAuthUrl}/validate");
            RestRequest request = new();
            request.AddHeader("Authorization", $"Bearer {currentUser.AccessToken}");

            RestResponse response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) return BadRequestResponse(detail: response.Content?.FromJson<dynamic>());
            if (response.Content is null) return BadRequestResponse(detail: "Invalid response from Twitch.");
            
            return Ok();
        }
        catch
        {
            return Unauthorized();
        }
    }
    
    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthenticatedResponse("User not logged in.");

        try
        {
            TokenResponse? refreshToken = await ApiClient.RefreshToken(currentUser);
            if (refreshToken == null) return UnauthorizedResponse("Failed to refresh token.");

            return Ok(new
            {
                user = new
                {
                    username = currentUser.Username,
                    displayName = currentUser.DisplayName,
                    profileImageUrl = currentUser.ProfileImageUrl,
                    accessToken = refreshToken.AccessToken,
                    refreshToken = refreshToken.RefreshToken,
                    tokenExpiry = refreshToken.ExpiresIn
                }
            });
        } catch
        {
            return Unauthorized();
        }
    }
    
    // Device grant
    [HttpGet("login")]
    public IActionResult Login()
    {
        string authorizationUrl = $"{Globals.TwitchAuthUrl}/authorize?response_type=code" +
                                  $"&client_id={Globals.TwitchClientId}" +
                                  $"&redirect_uri={Uri.EscapeDataString(Globals.RedirectUri)}" +
                                  $"&scope={Uri.EscapeDataString(string.Join(' ', Globals.Scopes))}";

        return Redirect(authorizationUrl);
    }

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize()
    {
        RestClient client = new($"{Globals.TwitchAuthUrl}/device");
        RestRequest request = new("", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) return BadRequestResponse(detail: response.Content ?? "Failed to fetch device code from Twitch.");
        if (response.Content is null) return BadRequestResponse(detail: "Invalid response from Twitch.");

        DeviceCodeResponse? deviceCodeResponse = response.Content.FromJson<DeviceCodeResponse>();
        if (deviceCodeResponse == null) return BadRequestResponse(detail: response.Content);

        return Ok(new
        {
            Message = "Please log in with Twitch",
            VerificationUrl = deviceCodeResponse.VerificationUri,
            deviceCodeResponse.DeviceCode,
        });
    }

    [HttpPost("poll")]
    public async Task<IActionResult> PollForToken([FromBody] DeviceCodeRequest data)
    {
        // Poll for the token and save user details in the database
        RestClient client = new($"{Globals.TwitchAuthUrl}/token");
        RestRequest request = new("", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
        request.AddParameter("device_code", data.DeviceCode);
        request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) return BadRequestResponse(detail: response.Content ?? "Failed to fetch token from Twitch.");
        if (response.Content is null) return BadRequestResponse(detail: "Invalid response from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) return BadRequestResponse(detail: response.Content);

        // Fetch user info from Twitch
        UserInfo? userInfo = await ApiClient.FetchUserInfo(tokenResponse.AccessToken);
        if (userInfo == null) return BadRequestResponse(detail: "Failed to fetch user information.");

        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };

        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                _accessToken = newUser._accessToken,
                _refreshToken = newUser._refreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();

        AfterLogin afterLogin = new();
        await afterLogin.Invoke(user);

        return Ok(new
        {
            Message = "Moderator logged in successfully",
            User = new
            {
                user.Id,
                user.Username,
                user.TokenExpiry,
                user.ProfileImageUrl,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken
            }
        });
    }

}

public class DeviceCodeRequest
{
    [JsonProperty("device_code")] public string DeviceCode { get; set; } = string.Empty;
}

public class DeviceCodeResponse
{
    [JsonProperty("device_code")] public string DeviceCode { get; set; } = string.Empty;
    [JsonProperty("user_code")] public string UserCode { get; set; } = string.Empty;
    [JsonProperty("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
    [JsonProperty("interval")] public int Interval { get; set; }
}

public class TokenResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = string.Empty;
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
}

public class UserInfoResponse
{
    [JsonProperty("data")] public List<UserInfo> Data { get; set; } = [];
}

public class UserInfo
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("login")] public string Login { get; set; } = string.Empty;
    [JsonProperty("display_name")] public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("type")] public string Type { get; set; } = string.Empty;
    [JsonProperty("broadcaster_type")] public string BroadcasterType { get; set; } = string.Empty;
    [JsonProperty("description")] public string Description { get; set; } = string.Empty;
    [JsonProperty("profile_image_url")] public string ProfileImageUrl { get; set; } = string.Empty;
    [JsonProperty("offline_image_url")] public string OfflineImageUrl { get; set; } = string.Empty;
    [JsonProperty("view_count")] public int ViewCount { get; set; }
    [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
}