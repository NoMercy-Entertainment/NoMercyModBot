using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Providers.Twitch;
using ModBot.Server.Services.Bot.Model;

namespace ModBot.Server.Services.Bot;

// BotManager.cs
public class BotManager : IHostedService, IBotManager
{
    private readonly ILogger<BotManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly BotInitializationQueue _initQueue;
    private readonly ConcurrentDictionary<string, TwitchLibClient> _botClients = new();
    private readonly ConcurrentDictionary<string, TwitchApiClient> _apiClients = new();
    private Timer? _tokenRefreshTimer;
    
    private readonly IBotClientManager _botClientManager;

    public BotManager(
        ILogger<BotManager> logger,
        IServiceProvider serviceProvider,
        IBotClientManager botClientManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClientManager = botClientManager;
        _initQueue = new(
            serviceProvider.GetRequiredService<ILogger<BotInitializationQueue>>(),
            serviceProvider,
            botClientManager
        );
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the initialization queue
        await _initQueue.StartAsync(cancellationToken);

        // Start token refresh timer
        _tokenRefreshTimer = new(CheckTokenRefresh, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        await InitializeExistingBots();
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _tokenRefreshTimer.DisposeAsync();

        // Stop the initialization queue
        await _initQueue.StopAsync(cancellationToken);

        // Cleanup clients
        foreach (TwitchLibClient client in _botClients.Values)
        {
            client.Dispose();
        }
        _botClients.Clear();
        _apiClients.Clear();
    }

    private async void CheckTokenRefresh(object? state)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<User> users = await dbContext.Users
                .Where(u => u.AccessToken != null && u.TokenExpiry != null)
                .Where(u => u.TokenExpiry < DateTime.UtcNow.AddMinutes(5))
                .ToListAsync();

            await Parallel.ForEachAsync(users,
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (user, token) =>
                {
                    try
                    {
                        await RefreshUserToken(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing token for user {UserId}", user.Id);
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in token refresh check");
        }
    }

    private async Task RefreshUserToken(User user)
    {
        await TwitchApiClient.RefreshToken(user);

        // Update API client token
        if (_apiClients.TryGetValue(user.Id, out TwitchApiClient? apiClient))
        {
            apiClient.UpdateToken(user.AccessToken!);
        }

        // Update bot client tokens
        foreach (TwitchLibClient botClient in _botClients.Values
                     .Where(c => c.User.Id == user.Id))
        {
            botClient.UpdateToken(user.AccessToken!);
        }
    }

    private async Task InitializeExistingBots()
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<User> users = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.AccessToken != null)
                .Include(u => u.ModeratorChannels)
                    .ThenInclude(moderator => moderator.Broadcaster)
                .ToListAsync();

            foreach (User user in users)
            {
                await InitializeUserBots(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing existing bots");
        }
    }

    public async Task InitializeUserBots(User user)
    {
        BotInitializationJob job = new(user);
        await _initQueue.QueueJob(job);
    }

}