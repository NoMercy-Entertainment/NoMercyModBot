using ModBot.Database.Models;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ModBot.Server.Services.Twitch;

public class TwitchChatService: IDisposable
{
    private readonly TwitchAuthService _twitchAuth;
    private readonly ILogger<TwitchChatService> _logger;
    private readonly Dictionary<string, (TwitchClient Client, HashSet<object> Listeners)> _clients = [];
    
    public TwitchChatService(TwitchAuthService twitchAuth, ILogger<TwitchChatService> logger)
    {
        _twitchAuth = twitchAuth;
        _logger = logger;
    }
    
    private string GetKey(User user, string channel) => $"{user.Username.ToLower()}:{channel.ToLower()}";

    public TwitchClient GetClient(User authModel, object listener, string channel)
    {
        TwitchClient client;

        lock (_clients)
        {
            string key = GetKey(authModel, channel);
            if (_clients.TryGetValue(key, out (TwitchClient Client, HashSet<object> Listeners) existing))
            {
                return existing.Client;
            }
            
            ConnectionCredentials credentials = new(authModel.Username, authModel.AccessToken);
            ClientOptions clientOptions = new()
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = null
            };
            WebSocketClient customClient = new(clientOptions);
            client = new(customClient);
            client.Initialize(credentials, channel);

            client.WillReplaceEmotes = false;

            client.OnConnected += (_, _) =>
            {
                // _logger.LogInformation("[{authModel.Username}] Connected", authModel.Username);
                foreach (JoinedChannel? x in client.JoinedChannels)
                    client.JoinChannel(x.Channel);
            };
            client.OnJoinedChannel += (_, ev) => _logger.LogInformation("[{authModel.Username}] Channel {ev.Channel} joined", authModel.Username, ev.Channel);
            client.OnLeftChannel += (_, ev) => _logger.LogWarning("[{authModel.Username}] Channel {ev.Channel} left", authModel.Username, ev.Channel);
            client.OnDisconnected += (_, _) =>
            {
                _logger.LogInformation("[{authModel.Username}] Disconnected", authModel.Username);
                lock (_twitchAuth)
                {
                    User? resAuth = _twitchAuth.GetRestoredById(key);
                    if (resAuth is not null)
                    {
                        credentials = new(resAuth.Username, resAuth.AccessToken);
                        client.GetType().GetProperty(nameof(client.ConnectionCredentials))?.SetValue(client, credentials);
                    }
                }
            };
            
            client.Connect();

            _clients[key] = (client, [listener]);
        }

        return client;
    }

    public void Unlisten(User authModel, object listener, string channel)
    {
        lock (_clients)
        {
            if (_clients.ContainsKey(GetKey(authModel, channel)))
            {
                if (_clients[GetKey(authModel, channel)].Listeners.Contains(listener))
                    _clients[GetKey(authModel, channel)].Listeners.Remove(listener);

                if (_clients[GetKey(authModel, channel)].Listeners.Count == 0)
                {
                    _clients[GetKey(authModel, channel)].Client.Disconnect();
                    _clients.Remove(GetKey(authModel, channel));
                }
            }
        }
    }

    
    public void Dispose()
    {
        lock (_clients)
        {
            foreach (KeyValuePair<string, (TwitchClient Client, HashSet<object> Listeners)> kv in _clients)
                kv.Value.Client.Disconnect();
            _clients.Clear();
        }
    }

    public Task JoinChannel(string channelId)
    {
        
        return Task.CompletedTask;
    }

    public Task LeaveChannel(string channelId)
    {
        
        return Task.CompletedTask;
    }

    public Task SendMessage(string channelId, string message)
    {
        
        return Task.CompletedTask;
    }
}