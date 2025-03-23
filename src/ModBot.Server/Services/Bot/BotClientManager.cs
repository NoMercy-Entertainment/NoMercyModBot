using System.Collections.Concurrent;
using ModBot.Database.Models;
using ModBot.Server.Providers.Twitch;

namespace ModBot.Server.Services.Bot;

public class BotClientManager : IBotClientManager
{
    private readonly ConcurrentDictionary<string, TwitchLibClient> _botClients = new();
    private readonly ConcurrentDictionary<string, TwitchApiClient> _apiClients = new();

    public void AddApiClient(string userId, TwitchApiClient client)
        => _apiClients.TryAdd(userId, client);

    public void AddBotClient(string key, TwitchLibClient client)
        => _botClients.TryAdd(key, client);

    public TwitchApiClient? GetApiClient(string userId)
    {
        _apiClients.TryGetValue(userId, out TwitchApiClient? client);
        return client;
    }

    public IEnumerable<TwitchLibClient> GetBotClientsForUser(string userId)
        => _botClients.Values.Where(c => c.User.Id == userId);

    public void UpdateUserToken(User user)
    {
        if (_apiClients.TryGetValue(user.Id, out TwitchApiClient? apiClient))
        {
            apiClient.UpdateToken(user.AccessToken!);
        }

        foreach (TwitchLibClient botClient in GetBotClientsForUser(user.Id))
        {
            botClient.UpdateToken(user.AccessToken!);
        }
    }

    public void Cleanup()
    {
        foreach (TwitchLibClient client in _botClients.Values)
        {
            client.Dispose();
        }
        _botClients.Clear();
        _apiClients.Clear();
    }
}