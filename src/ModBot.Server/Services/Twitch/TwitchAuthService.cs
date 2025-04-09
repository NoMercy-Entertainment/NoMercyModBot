// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

using System.Collections.Specialized;
using System.Web;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;
using ModBot.Server.Helpers;
using RestSharp;

namespace ModBot.Server.Services.Twitch;

public class TwitchAuthService
{
    private readonly IServiceScope _scope;
    private readonly IConfiguration _conf;
    private readonly ILogger<TwitchAuthService> _logger;
    private readonly AppDbContext _db;
    private readonly TwitchApiService _api;

    public TwitchAuthService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<TwitchAuthService> logger, TwitchApiService api)
    {
        _scope = serviceScopeFactory.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
        _api = api;
    }

    public async Task<TokenResponse> Callback(string code)
    {
        RestClient restClient = new(Globals.TwitchAuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", Globals.TwitchClientId);
                    request.AddParameter("client_secret", Globals.ClientSecret);
                    request.AddParameter("code", code);
                    request.AddParameter("scope", string.Join(' ', Globals.Scopes));
                    request.AddParameter("grant_type", "authorization_code");
                    request.AddParameter("redirect_uri", Globals.RedirectUri);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
        
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return tokenResponse;
    }
    
    public async Task<TokenResponse> ValidateToken(string accessToken)
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        RestRequest request = new("validate");
                    request.AddHeader("Authorization", $"Bearer {accessToken}");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
            
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return tokenResponse;
    }

    public Task<TokenResponse> ValidateToken(HttpRequest request)
    {
        string authorizationHeader = request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];
        
        return ValidateToken(accessToken);
    }
    
    public async Task<TokenResponse> RefreshToken(string refreshToken)
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", Globals.TwitchClientId);
                    request.AddParameter("client_secret", Globals.ClientSecret);
                    request.AddParameter("refresh_token", refreshToken);
                    request.AddParameter("grant_type", "refresh_token");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");

        TokenResponse? tokenResponse = response.Content?.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return tokenResponse;
    }
    
    public async Task RevokeToken(string accessToken)
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        
        RestRequest request = new("revoke", Method.Post);
                    request.AddParameter("client_id", Globals.TwitchClientId);
                    request.AddParameter("token", accessToken);
                    request.AddParameter("token_type_hint", "access_token");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
    }
    
    public string GetRedirectUrl()
    {
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
                            query.Add("response_type", "code");
                            query.Add("client_id", Globals.TwitchClientId);
                            query.Add("redirect_uri", Globals.RedirectUri);
                            query.Add("scope", string.Join(' ', Globals.Scopes));
        
        UriBuilder uriBuilder = new(Globals.TwitchAuthUrl + "/authorize")
        {
            Query = query.ToString(),
            Scheme = Uri.UriSchemeHttps,
        };
        
        return uriBuilder.ToString();
    }

    public async Task<DeviceCodeResponse> Authorize()
    {
        RestClient client = new(Globals.TwitchAuthUrl);
        
        RestRequest request = new("device", Method.Post);
                    request.AddParameter("client_id", Globals.TwitchClientId);
                    request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch device code from Twitch.");

        DeviceCodeResponse? deviceCodeResponse = response.Content.FromJson<DeviceCodeResponse>();
        if (deviceCodeResponse == null) throw new("Invalid response from Twitch.");

        return deviceCodeResponse;
    }
    
    public async Task<TokenResponse> PollForToken(string deviceCode)
    {
        RestClient restClient = new(Globals.TwitchAuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", Globals.TwitchClientId);
                    request.AddParameter("client_secret", Globals.ClientSecret);
                    request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
                    request.AddParameter("device_code", deviceCode);
                    request.AddParameter("scopes", string.Join(' ', Globals.Scopes));

        RestResponse response = await restClient.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch token from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return tokenResponse;
    }

    public User? GetRestoredById(string authModelId)
    {
        lock (_db)
        {
            return _db.Users.FirstOrDefault(u => u.Id == authModelId);
        }
    }
}