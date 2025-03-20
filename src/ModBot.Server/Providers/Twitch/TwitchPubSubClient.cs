using ModBot.Database;
using ModBot.Database.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ModBot.Server.Providers.Twitch;

public class TwitchPubSubClient
{
    private static readonly Dictionary<string, TwitchPubSub> Clients = [];

    public TwitchPubSubClient(User user)
    {
        if (Clients.Any(client => client.Key == user.Id)) return;

        TwitchPubSub client = new();
        client.OnPubSubServiceConnected += OnPubSubServiceConnected;
        client.OnListenResponse += OnListenResponse;
        client.OnStreamUp += OnStreamUp;
        client.OnStreamDown += OnStreamDown;
        
        client.SendTopics();
        client.Connect();

        Clients.Add(user.Id, client);
    }
    
    private void OnPubSubServiceConnected(object? sender, EventArgs e)
    {
        // SendTopics accepts an oauth optionally, which is necessary for some topics
        Clients.First(client => client.Value == (TwitchPubSub) sender).Value.SendTopics();
    }
        
    private static void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        if (!e.Successful)
            throw new Exception($"Failed to listen! Response: {e.Response}");
    }

    private static void OnStreamUp(object? sender, OnStreamUpArgs e)
    {
        Console.WriteLine($"Stream just went up! Play delay: {e.PlayDelay}, server time: {e.ServerTime}");
        
        using AppDbContext dbContext = new();
        User user = dbContext.Users.First(user => user.Id == e.ChannelId);
        user.IsLive = true;
        dbContext.SaveChanges();
    }

    private static void OnStreamDown(object? sender, OnStreamDownArgs e)
    {
        Console.WriteLine($"Stream just went down! Server time: {e.ServerTime}");
        
        using AppDbContext dbContext = new();
        User user = dbContext.Users.First(user => user.Id == e.ChannelId);
        user.IsLive = false;
        dbContext.SaveChanges();
    }
}