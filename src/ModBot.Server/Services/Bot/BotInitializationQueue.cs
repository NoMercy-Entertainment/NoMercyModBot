using System.Threading.Channels;
using ModBot.Server.Providers.Twitch;
using ModBot.Server.Services.Bot.Model;

namespace ModBot.Server.Services.Bot;

public class BotInitializationQueue : BackgroundService
{
    private readonly ILogger<BotInitializationQueue> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBotClientManager _botClientManager;
    private readonly Channel<BotInitializationJob> _queue;

    public BotInitializationQueue(
        ILogger<BotInitializationQueue> logger,
        IServiceProvider serviceProvider,
        IBotClientManager botClientManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClientManager = botClientManager;
        _queue = Channel.CreateUnbounded<BotInitializationJob>();
    }

    public async Task QueueJob(BotInitializationJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                BotInitializationJob job = await _queue.Reader.ReadAsync(stoppingToken);
                await ProcessJob(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bot initialization job");
            }
        }
    }

    private async Task ProcessJob(BotInitializationJob job, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Initializing Twitch API client for {username}", job.User.Username);

            if (string.IsNullOrEmpty(job.User.AccessToken))
            {
                throw new InvalidOperationException($"Access token is null for user {job.User.Username}");
            }

            // Initialize API client
            TwitchApiClient apiClient = new(job.User);
            _botClientManager.AddApiClient(job.User.Id, apiClient);

            // Initialize and connect own channel first
            TwitchLibClient selfBot = new(user: job.User, broadcaster: job.User);
            _botClientManager.AddBotClient($"{job.User.Id}:{job.User.Id}", selfBot);
            await selfBot.Initialize();

            IEnumerable<Task> channelTasks = job.User.ModeratorChannels
                // .Where(channel => channel.Broadcaster?.Id != job.User.Id)
                .Select(async channel =>
                {
                    try
                    {
                        TwitchLibClient channelBot = new(user: job.User, broadcaster: channel.Broadcaster);
                        _botClientManager.AddBotClient($"{job.User.Id}:{channel.BroadcasterId}", channelBot);
                        await channelBot.Initialize();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize bot for channel {channel}", channel.Broadcaster.Username);
                    }
                });

            // Wait for all channel connections with a timeout
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

            try
            {
                await Task.WhenAll(channelTasks);
                job.CompletionSource.SetResult(true);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Channel initialization timed out for user {username}", job.User.Username);
                job.CompletionSource.SetResult(true); // Consider partial success
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing bots for user {UserId}", job.User.Id);
            job.CompletionSource.SetException(ex);
            throw;
        }
    }
}