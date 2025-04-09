using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;

namespace ModBot.Server.Providers.Twitch;

public class TwitchBotAuth
{
    private static TokenResponse? _botToken;    
    private static DateTime _tokenExpiresAt;

    internal static User BotUser { get; set; } = null!;
    private static readonly SemaphoreSlim Semaphore = new(1);
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TwitchBotAuth>();
    
    public TwitchBotAuth()
    {
        TokenResponse token = TwitchApiClient.BotToken().Result;
        _botToken = new()
        {
            AccessToken = token.AccessToken,
            ExpiresIn = token.ExpiresIn
        };
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
        
        BotUser = TwitchApiClient.FetchUser(tokenResponse: token, id: Globals.TwitchBotId).Result;
    }
    
    public static async Task<TokenResponse?> GetBotToken()
    {
        try
        {
            await Semaphore.WaitAsync();

            if (_botToken != null && _tokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return _botToken;
            }

            _botToken = await FetchNewBotToken();
            return _botToken;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task<TokenResponse?> FetchNewBotToken()
    {
        TokenResponse token = await TwitchApiClient.BotToken();
        _botToken = new()
        {
            AccessToken = token.AccessToken,
            ExpiresIn = token.ExpiresIn
        };
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
        
        BotUser = await TwitchApiClient.FetchUser(tokenResponse: token, id: Globals.TwitchBotId);
        return _botToken;
    }
}