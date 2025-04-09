using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers;
using ModBot.Server.Hubs;
using ModBot.Server.Providers.Twitch;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;
using ModBotChatMessage = ModBot.Database.Models.ChatMessage;


namespace ModBot.Server.Services.Twitch;

public class TwitchChatMessagesService
{
    private static readonly Dictionary<string, (string code, string name, string slug)> CODE_LANGUAGES = new()
    {
        { "cs", ("cs", "c#", "csharp") },
        { "js", ("js", "javascript", "javascript") },
        { "ts", ("ts", "typescript", "typescript") },
        { "css", ("css", "css", "css") },
        { "html", ("html", "html", "html") },
        { "json", ("json", "json", "json") },
        { "java", ("java", "java", "java") },
        { "cpp", ("cpp", "c++", "cpp") }
    };
    
    private readonly ConcurrentDictionary<string, HashSet<string>> _channelConnections = new();
    private readonly ConcurrentDictionary<string, (TwitchClient client, User user)> _usernameToClient = new();

    public event Action<ModBotChatMessage>? OnMessage;
    public event Action<string>? OnMessageDelete;
    public event Action<string>? OnUserSuspend;

    private readonly TwitchChatService _chat;
    private readonly TwitchCommandsService _commands;
    private readonly ILogger<TwitchChatService> _logger;
    private readonly TwitchEmotesService _emotes;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<ChatHub> _hubContext;
    
    public TwitchChatMessagesService(
        IHubContext<ChatHub> hubContext,
        TwitchChatService chat,
        TwitchCommandsService commands,
        ILogger<TwitchChatService> logger,
        TwitchEmotesService emotes)
    {        
        _hubContext = hubContext;
        _chat = chat;
        _commands = commands;
        _logger = logger;
        _emotes = emotes;
        _dbContext = new();
    }
    
    private string GetKey(User user, string channel) => $"{user.Username.ToLower()}:{channel.ToLower()}";

    public void Connect(User user, string channel)
    {
        if (_usernameToClient.ContainsKey(GetKey(user, channel)))
            return;

        TwitchClient client = _chat.GetClient(user, this, channel);
        client.OnMessageReceived += OnMessageReceived;
        
        client.OnMessageCleared += OnMessageCleared;
        client.OnUserTimedout += OnUserTimedOut;
        client.OnUserBanned += OnUserBanned;
        client.OnError += ClientError;

        _usernameToClient.AddOrUpdate(GetKey(user, channel), (client, user), (_, _) => (client, user));
    }

    public void SendMessage(string username, string channel, string message)
    {
        if (_usernameToClient.TryGetValue(username.ToLower(), out (TwitchClient client, User user) item))
            item.client.SendMessage(channel, message);
    }

    public void SendMessage(string username, string channel, string replyId, string message)
    {
        if (_usernameToClient.TryGetValue(username.ToLower(), out (TwitchClient client, User user) item))
            item.client.SendReply(channel, replyId, message);
    }

    public void Disconnect(User user, string channel)
    {
        if (!_usernameToClient.TryGetValue(GetKey(user, channel), out (TwitchClient client, User user) item))
            return;

        item.client.LeaveChannel(channel);
        if (item.client.JoinedChannels.Any())
            return;

        item.client.OnMessageReceived -= OnMessageReceived;
        item.client.OnMessageCleared -= OnMessageCleared;
        item.client.OnUserTimedout -= OnUserTimedOut;
        item.client.OnUserBanned -= OnUserBanned;
        item.client.OnError -= ClientError;

        _chat.Unlisten(user, this, channel);

        _usernameToClient.TryRemove(GetKey(user, channel), out _);
    }
    
    public void AddMessageHandler(string channelName, string connectionId)
    {
        _channelConnections.AddOrUpdate(
            channelName,
            [connectionId],
            (_, connections) =>
            {
                connections.Add(connectionId);
                return connections;
            });
    }

    public void RemoveMessageHandler(string channelName, string connectionId)
    {
        if (!_channelConnections.TryGetValue(channelName, out HashSet<string>? connections)) return;
        connections.Remove(connectionId);
        if (connections.Count == 0)
        {
            _channelConnections.TryRemove(channelName, out _);
        }
    }
    
    private async void OnMessageReceived(object? sender, OnMessageReceivedArgs ev)
    {
        try
        {
            _logger.LogInformation($"{ev.ChatMessage.DisplayName}: {ev.ChatMessage.Message}");
        
            ModBotChatMessage chatMessage = new(ev.ChatMessage);
            
            // Notify channel handlers using HubContext
            if (_channelConnections.TryGetValue(ev.ChatMessage.Channel, out HashSet<string> _))
            {
                await _hubContext.Clients.Group(ev.ChatMessage.Channel)
                    .SendAsync("ReceiveMessage", chatMessage);
            }
        
            await Task.Run(async () =>
            {
                try
                {
                    if (!_dbContext.Users.Any(u => u.Id == ev.ChatMessage.UserId))
                    {
                        TokenResponse? botToken = await TwitchBotAuth.GetBotToken();
                        if (botToken?.AccessToken == null)
                        {
                            _logger.LogError("Bot token not available for user lookup");
                            return;
                        }

                        await TwitchApiClient.FetchUser(new()
                        {
                            AccessToken = botToken.AccessToken
                        }, id: ev.ChatMessage.UserId);
                    }
                
                    await _dbContext.ChatMessages.Upsert(chatMessage)
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
                        .RunAsync();
                      
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error processing message");
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing message");
        }
    }

    private void OnMessageCleared(object? sender, OnMessageClearedArgs ev)
    {
        // _tts.DeleteById(ev.TargetMessageId);
        OnMessageDelete?.Invoke(ev.TargetMessageId);
    }

    private void OnUserTimedOut(object? sender, OnUserTimedoutArgs ev)
    {
        // _tts.DeleteByUsername(ev.OnUserTimedOut.Username);
        OnUserSuspend?.Invoke(ev.UserTimeout.Username);
    }

    private void OnUserBanned(object? sender, OnUserBannedArgs ev)
    {
        // _tts.DeleteByUsername(ev.UserBan.Username);
        OnUserSuspend?.Invoke(ev.UserBan.Username);
    }

    private void ClientError(object? sender, OnErrorEventArgs ev)
    {
        _logger.LogError(ev.Exception.Message);
    }

    private List<string> SplitMessageIntoChunks(string message, int chunkLength)
    {
        List<string> chunks = [];
        if (string.IsNullOrEmpty(message) || chunkLength <= 0)
            return chunks;

        string[] sentences = Regex.Split(message, @"(?<=[.!?])\s+");
        StringBuilder currentChunk = new();

        foreach (string? sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length + 1 <= chunkLength)
            {
                if (0 < currentChunk.Length)
                    currentChunk.Append(" ");
                currentChunk.Append(sentence);
            }
            else
            {
                if (0 < currentChunk.Length)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                if (sentence.Length <= chunkLength)
                    currentChunk.Append(sentence);
                else
                    SplitSentenceIntoChunks(sentence, chunkLength, chunks);
            }
        }

        if (0 < currentChunk.Length)
            chunks.Add(currentChunk.ToString().Trim());

        return chunks;
    }
    private void SplitSentenceIntoChunks(string sentence, int chunkLength, List<string> chunks)
    {
        string[] words = sentence.Split(' ');
        StringBuilder currentChunk = new();

        foreach (string? word in words)
        {
            if (chunkLength < word.Length)
            {
                if (0 < currentChunk.Length)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                SplitWordIntoChunks(word, chunkLength, chunks);
            }
            else
            {
                if (currentChunk.Length + word.Length + 1 <= chunkLength)
                {
                    if (0 < currentChunk.Length)
                        currentChunk.Append(" ");
                    currentChunk.Append(word);
                }
                else
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                    currentChunk.Append(word);
                }
            }
        }

        if (0 < currentChunk.Length)
            chunks.Add(currentChunk.ToString().Trim());
    }
    private void SplitWordIntoChunks(string word, int chunkLength, List<string> chunks)
    {
        for (int i = 0; i < word.Length; i += chunkLength)
        {
            int length = Math.Min(chunkLength, word.Length - i);
            chunks.Add(word.Substring(i, length));
        }
    }

}