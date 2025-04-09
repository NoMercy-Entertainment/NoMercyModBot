using ModBot.Server.Providers.Twitch;

namespace ModBot.Server.Services.Bot;

public interface IBotClientManager
{
    void AddApiClient(string userId, TwitchApiClient client);
    void AddBotClient(string key, TwitchLibClient client);
}