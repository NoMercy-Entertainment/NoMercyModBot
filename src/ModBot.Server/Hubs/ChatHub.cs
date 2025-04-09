using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;
using ModBot.Server.Services.Twitch;
using TwitchLib.Client;
using Hub = Microsoft.AspNetCore.SignalR.Hub;
using ModBotChatMessage = ModBot.Database.Models.ChatMessage;

namespace ModBot.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChatHub> _logger;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchChatMessagesService _twitchChatMessages;
    private readonly AppDbContext _dbContext;
    private static readonly Dictionary<string, HashSet<string>> _channelConnections = new();
    private readonly Dictionary<string, Action<ChatMessage>> _messageHandlers = new();

    public ChatHub(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChatHub> logger,
        TwitchChatService twitchChatService,
        TwitchChatMessagesService twitchChatMessages,
        AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _twitchChatService = twitchChatService;
        _twitchChatMessages = twitchChatMessages;
        _dbContext = dbContext;
    }

    public async Task JoinChannel(string channelName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, channelName);

            if (!_channelConnections.ContainsKey(channelName))
            {
                _channelConnections[channelName] = [];

                User? user = Context.User.User();
                if (user != null)
                {
                    _twitchChatService.GetClient(user, this, channelName);
                    _twitchChatMessages.Connect(user, channelName);
                }
            }

            _twitchChatMessages.AddMessageHandler(channelName, Context.ConnectionId);
            _channelConnections[channelName].Add(Context.ConnectionId);

            _logger.LogInformation("Client {ConnectionId} joined channel {ChannelName}", 
                Context.ConnectionId, channelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining channel {ChannelName}", channelName);
            throw;
        }
    }

    public async Task LeaveChannel(string channelName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName);

            if (_channelConnections.ContainsKey(channelName))
            {
                _channelConnections[channelName].Remove(Context.ConnectionId);

                if (_channelConnections[channelName].Count == 0)
                {
                    _channelConnections.Remove(channelName);

                    User? user = Context.User.User();
                    if (user != null)
                    {
                        _twitchChatService.Unlisten(user, this, channelName);
                    }
                }
            }

            _twitchChatMessages.RemoveMessageHandler(channelName, Context.ConnectionId);

            _logger.LogInformation("Client {ConnectionId} left channel {ChannelName}", 
                Context.ConnectionId, channelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving channel {ChannelName}", channelName);
            throw;
        }
    }

    public async Task SendMessage(ChatMessage message)
    {
        try
        {
            User? user = Context.User.User();
            if (user != null)
            {
                TwitchClient client = _twitchChatService.GetClient(user, this, message.BotUsername);
                client.SendMessage(message.BotUsername, message.Message);

                ModBotChatMessage firstMessage = _dbContext.ChatMessages
                    .OrderByDescending(m => m.TmiSentTs)
                    .First(m => m.UserId == user.Id && m.ChannelId == message.ChannelId);
                
                message.Badges = firstMessage.Badges;
                message.Color = firstMessage.Color;
                message.ColorHex = firstMessage.ColorHex;
                message.DisplayName = firstMessage.DisplayName;
                message.BadgeInfo = firstMessage.BadgeInfo;
                
                await BroadcastMessage(message.BotUsername, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to channel {ChannelName}", message.BotUsername);
            throw;
        }
    }

    public async Task BroadcastMessage(string channelName, ChatMessage message)
    {
        await Clients.Group(channelName).SendAsync("ReceiveMessage", message);
    }
    
    public override async Task OnConnectedAsync()
    {
        User? user = Context.User.User();
        _logger.LogInformation($"User {user?.Username} connected to chat");
        
        await base.OnConnectedAsync();
    }
}