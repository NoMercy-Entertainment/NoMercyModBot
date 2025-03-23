using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using ChatMessage = ModBot.Database.Models.ChatMessage;

namespace ModBot.Server.Providers.Twitch;

public class TwitchLibClient: IDisposable
{
    private readonly TwitchClient _client;
    private readonly AppDbContext _dbContext;
    private readonly ConnectionCredentials credentials;
    internal User User { get; }
    private User Broadcaster { get; }

    public TwitchLibClient(User user, User broadcaster)
    {
        User = user;
        Broadcaster = broadcaster;
        
        _dbContext = new();
        credentials = new(user.Username, user.AccessToken);
        ClientOptions clientOptions = new();

        WebSocketClient customClient = new(clientOptions)
        {
            Options =
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            }
        };
        
        _client = new(customClient);
    }
    
    public void UpdateToken(string newToken)
    {
        // Update the token in your chat client
    }

    public void Dispose()
    {
        // Cleanup resources
    }

    private void Client_OnError(object? sender, OnErrorEventArgs e)
    {
        Console.WriteLine(e.Exception.Message);
    }

    private void Client_OnLog(object? sender, OnLogArgs e)
    {
        // Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        // Console.WriteLine($"{e.BotUsername} connected to {e.AutoJoinChannel}");
    }

    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"{e.BotUsername} joined channel {e.Channel}");
        
        // _client.SendMessage(e.Channel, "Hey guys! I am a bot connected via TwitchLib!");
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (!_dbContext.Users.Any(u => u.Id == e.ChatMessage.UserId))
        {
            TwitchApiClient.FetchUser(new()
            {
                AccessToken = TwitchBotAuth.BotToken.AccessToken,
            }, id: e.ChatMessage.UserId).Wait();
        }
        ChatMessage chatMessage = new(e.ChatMessage);
        
        _dbContext.ChatMessages.Upsert(chatMessage)
            .On(u => new { u.Id })
            .WhenMatched((old, incoming) => new()
            {
                BadgeInfo = incoming.BadgeInfo,
                Badges = incoming.Badges,
                ChannelId = incoming.ChannelId,
                Bits = incoming.Bits,
                BitsInDollars = incoming.BitsInDollars,
                BotUsername = incoming.BotUsername,
                CheerBadge = incoming.CheerBadge,
                Color = incoming.Color,
                ColorHex = incoming.ColorHex,
                CustomRewardId = incoming.CustomRewardId,
                DisplayName = incoming.DisplayName,
                EmoteSet = incoming.EmoteSet,
                Id = incoming.Id,
                IsBroadcaster = incoming.IsBroadcaster,
                IsFirstMessage = incoming.IsFirstMessage,
                IsHighlighted = incoming.IsHighlighted,
                IsMe = incoming.IsMe,
                IsModerator = incoming.IsModerator,
                IsPartner = incoming.IsPartner,
                IsSkippingSubMode = incoming.IsSkippingSubMode,
                IsStaff = incoming.IsStaff,
                IsSubscriber = incoming.IsSubscriber,
                IsTurbo = incoming.IsTurbo,
                IsVip = incoming.IsVip,
                Message = incoming.Message,
                Noisy = incoming.Noisy,
                ReplyToMessageId = incoming.ReplyToMessageId,
                SubscribedMonthCount = incoming.SubscribedMonthCount,
                TmiSentTs = incoming.TmiSentTs,
                UserId = incoming.UserId,
                UserType = incoming.UserType,
                Username = incoming.Username,
            })
            .Run();
    }
    
    private void Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
    {
        Console.WriteLine(e.WhisperMessage.ToJson());
    }
    
    private void Client_OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        Console.WriteLine(e.Subscriber.ToJson());
        
        if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            _client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
        else
            _client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
    }

    public Task Initialize()
    {
        try
        {
            _client.Initialize(credentials, Broadcaster.Username);

            _client.OnLog += Client_OnLog;
            _client.OnError += Client_OnError;
            _client.OnJoinedChannel += Client_OnJoinedChannel;
            _client.OnMessageReceived += Client_OnMessageReceived;
            _client.OnWhisperReceived += Client_OnWhisperReceived;
            _client.OnNewSubscriber += Client_OnNewSubscriber;
            _client.OnConnected += Client_OnConnected;

            _client.Connect();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
        return Task.CompletedTask;
    }
}