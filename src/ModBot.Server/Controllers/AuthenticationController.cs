using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers.Dto;
using ModBot.Server.Helpers;
using ModBot.Server.Middlewares;
using ModBot.Server.Providers.Twitch;
using RestSharp;
using Newtonsoft.Json;
using NodaTime.TimeZones;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;

namespace ModBot.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(AppDbContext dbContext) : BaseController
{
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error)) return BadRequestResponse(error);

        RestClient restClient = new($"{Globals.TwitchAuthUrl}/token");
        RestRequest request = new("", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("code", code);
        request.AddParameter("scope", string.Join(' ', Globals.Scopes));
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("redirect_uri", Globals.RedirectUri);

        RestResponse response = await restClient.ExecuteAsync(request);
        if (!response.IsSuccessful)
            return BadRequestResponse(response.Content ?? "Failed to fetch token from Twitch.");
        if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) return BadRequestResponse(response.Content);

        UserInfo? userInfo = await TwitchApiClient.FetchUserInfo(tokenResponse.AccessToken);
        if (userInfo == null) return BadRequestResponse("Failed to fetch user information.");

        string countryCode = HttpContext.Request.Headers["CF-IPCountry"]!;
        
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
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            Timezone = zoneIds?.FirstOrDefault(),
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };
        
        
        _ = new TwitchApiClient(user);
        Helix client = TwitchApiClient.GetHelixClient(user);
        GetUserChatColorResponse colors = await client.Chat.GetUserChatColorAsync([userInfo.Id]);
        
        string color = colors.Data.First().Color;
        
        user.Color = string.IsNullOrEmpty(color)
            ? "#9146FF"
            : color;

        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                OfflineImageUrl = newUser.OfflineImageUrl,
                Color = newUser.Color,
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
            User = new UserWithTokenDto(user, tokenResponse),
        });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateSession()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthenticatedResponse("User not logged in.");

        try
        {
            RestClient client = new(Globals.TwitchAuthUrl);
            RestRequest request = new("validate");
            request.AddHeader("Authorization", $"Bearer {currentUser.AccessToken}");

            RestResponse response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) return BadRequestResponse(response.Content);
            if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");

            return Ok();
        }
        catch
        {
            return UnauthorizedResponse("Failed to validate session.");
        }
    }

    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthenticatedResponse("User not logged in.");

        try
        {
            TokenResponse? tokenResponse = await TwitchApiClient.RefreshToken(currentUser);
            if (tokenResponse == null) return UnauthorizedResponse("Failed to refresh token.");

            return Ok(new
            {
                Message = "Moderator logged in successfully",
                User = new UserWithTokenDto(currentUser, tokenResponse),
            });
        }
        catch
        {
            return Unauthorized();
        }
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        User? currentUser = User.User();
        if (currentUser == null) return UnauthenticatedResponse("User not logged in.");

        try
        {
            // Revoke Twitch token
            RestClient client = new($"{Globals.TwitchAuthUrl}/revoke");
            RestRequest request = new("", Method.Post);
            request.AddParameter("client_id", Globals.TwitchClientId);
            request.AddParameter("token", currentUser.AccessToken);

            await client.ExecuteAsync(request);

            User user = await dbContext.Users
                .Include(x => x.BroadcasterChannels)
                .Include(x => x.ModeratedChannels)
                .Include(x => x.BroadcasterBlockedTerms)
                .Include(x => x.ModeratorBlockedTerms)
                .FirstAsync(u => u.Id == currentUser.Id);

            // Remove relationships explicitly
            List<Channel> broadcasterChannels = user.BroadcasterChannels.ToList();
            List<Channel> moderatedChannels = user.ModeratedChannels.ToList();
            List<BlockedTerm> broadcasterTerms = user.BroadcasterBlockedTerms.ToList();
            List<BlockedTerm> moderatorTerms = user.ModeratorBlockedTerms.ToList();

            dbContext.RemoveRange(broadcasterChannels);
            dbContext.RemoveRange(moderatedChannels);
            dbContext.RemoveRange(broadcasterTerms);
            dbContext.RemoveRange(moderatorTerms);

            // Clear auth data
            user.AccessToken = null;
            user.RefreshToken = null;
            user.TokenExpiry = null;

            await dbContext.SaveChangesAsync();

            return Ok(new { Message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
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
        if (!response.IsSuccessful)
            return BadRequestResponse(response.Content ?? "Failed to fetch device code from Twitch.");
        if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");

        DeviceCodeResponse? deviceCodeResponse = response.Content.FromJson<DeviceCodeResponse>();
        if (deviceCodeResponse == null) return BadRequestResponse(response.Content);

        return Ok(new
        {
            Message = "Please log in with Twitch",
            VerificationUrl = deviceCodeResponse.VerificationUri,
            deviceCodeResponse.DeviceCode
        });
    }

    [HttpPost("poll")]
    public async Task<IActionResult> PollForToken([FromBody] DeviceCodeRequest data)
    {
        // Poll for the token and save user details in the database
        RestClient restClient = new($"{Globals.TwitchAuthUrl}/token");
        RestRequest request = new("", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("client_secret", Globals.ClientSecret);
        request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
        request.AddParameter("device_code", data.DeviceCode);
        request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await restClient.ExecuteAsync(request);
        if (!response.IsSuccessful) return BadRequestResponse(response.Content ?? "Failed to fetch token from Twitch.");
        if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) return BadRequestResponse(response.Content);

        // Fetch user info from Twitch
        UserInfo? userInfo = await TwitchApiClient.FetchUserInfo(tokenResponse.AccessToken);
        if (userInfo == null) return BadRequestResponse("Failed to fetch user information.");
        
        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            DisplayName = userInfo.DisplayName,
            Description = userInfo.Description,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            OfflineImageUrl = userInfo.OfflineImageUrl,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };
        
        Helix client = TwitchApiClient.GetHelixClient(user);
        GetUserChatColorResponse colors = await client.Chat.GetUserChatColorAsync([userInfo.Id]);
        
        string color = colors.Data.First().Color;

        user.Color = string.IsNullOrEmpty(color)
            ? "#9146FF"
            : color;

        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                OfflineImageUrl = newUser.OfflineImageUrl,
                Color = newUser.Color,
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
            User = new UserWithTokenDto(user, tokenResponse),
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