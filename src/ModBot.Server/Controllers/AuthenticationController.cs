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
        if (tokenResponse is null) return BadRequestResponse(response.Content);
        
        string countryCode = HttpContext.Request.Headers["CF-IPCountry"].ToString();
        
        User user = await TwitchApiClient.FetchUser(tokenResponse, countryCode);

        try
        {
            AfterLogin afterLogin = new();
            await afterLogin.Invoke(user);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequestResponse(e.Message);
        }

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
        if (currentUser is null) return UnauthenticatedResponse("User not logged in.");
        
        string authorizationHeader = Request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];

        try
        {
            RestClient client = new(Globals.TwitchAuthUrl);
            RestRequest request = new("validate");
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            RestResponse response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) return BadRequestResponse(response.Content);
            if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");
            
            TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
            if (tokenResponse is null) return BadRequestResponse("Failed to get a new token");
            
            return Ok(new
            {
                Message = "Session validated successfully",
                User = new UserWithTokenDto(currentUser),
            });
        }
        catch
        {
            return UnauthorizedResponse("Failed to validate session.");
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest data)
    {
        User? currentUser = User.User();
        if (currentUser is null) return UnauthenticatedResponse("User not logged in.");
        
        currentUser.RefreshToken = data.RefreshToken;

        try
        {
            TokenResponse? tokenResponse = await TwitchApiClient.RefreshToken(currentUser);
            if (tokenResponse is null) return UnauthorizedResponse("Failed to refresh token.");
            
            User user = await dbContext.Users
                .FirstAsync(u => u.Id == currentUser.Id);
            user.AccessToken = tokenResponse.AccessToken;
            user.RefreshToken = tokenResponse.RefreshToken;
            user.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                Message = "Token refreshed successfully",
                User = new UserWithTokenDto(user, tokenResponse),
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
        if (currentUser is null) return UnauthenticatedResponse("User not logged in.");

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
                .Include(x => x.ModeratorChannels)
                .Include(x => x.BroadcasterBlockedTerms)
                .Include(x => x.ModeratorBlockedTerms)
                .FirstAsync(u => u.Id == currentUser.Id);

            // Remove relationships explicitly
            List<Channel> broadcasterChannels = user.BroadcasterChannels.ToList();
            List<Channel> moderatedChannels = user.ModeratorChannels.ToList();
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
        RestClient client = new(Globals.TwitchAuthUrl);
        RestRequest request = new("device", Method.Post);
        request.AddParameter("client_id", Globals.TwitchClientId);
        request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
            return BadRequestResponse(response.Content ?? "Failed to fetch device code from Twitch.");
        if (response.Content is null) return BadRequestResponse("Invalid response from Twitch.");

        DeviceCodeResponse? deviceCodeResponse = response.Content.FromJson<DeviceCodeResponse>();
        if (deviceCodeResponse is null) return BadRequestResponse(response.Content);

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
        if (tokenResponse is null) return BadRequestResponse(response.Content);

        // Fetch user info from Twitch
        UserInfo? userInfo = await TwitchApiClient.FetchUserInfo(tokenResponse.AccessToken);
        if (userInfo is null) return BadRequestResponse("Failed to fetch user information.");
        
        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            DisplayName = userInfo.DisplayName,
            Description = userInfo.Description,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            OfflineImageUrl = userInfo.OfflineImageUrl,
            BroadcasterType = userInfo.BroadcasterType,
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
                BroadcasterType = newUser.BroadcasterType,
                AccessToken = newUser.AccessToken,
                RefreshToken = newUser.RefreshToken,
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

public class RefreshRequest
{
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = null!;
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