using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Controllers;

namespace ModBot.Server.Providers.Twitch;

public class TwitchBotAuth
{
    internal static BotTokenResponse BotToken { get; set; } = null!;
    internal static User BotUser { get; set; } = null!;
    
    public TwitchBotAuth()
    {
        TokenResponse token = TwitchApiClient.BotToken().Result;
        BotToken = new()
        {
            AccessToken = token.AccessToken,
            ExpiresIn = token.ExpiresIn
        };

        BotUser = TwitchApiClient.FetchUser(tokenResponse: token, id: Globals.TwitchBotId).Result;
    }

    internal class BotTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
    
}