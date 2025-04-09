using Microsoft.EntityFrameworkCore;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Controllers;

namespace ModBot.Server.Services.Twitch;

public class TwitchTokenRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TwitchTokenRefreshService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public TwitchTokenRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<TwitchTokenRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Twitch token refresh service.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshTokens(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task RefreshTokens(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TwitchAuthService authService = scope.ServiceProvider.GetRequiredService<TwitchAuthService>();

        List<User> usersNeedingRefresh = await dbContext.Users
            .Where(u => u.AccessToken != null && u.RefreshToken != null)
            .Where(u => u.TokenExpiry != null && u.TokenExpiry < DateTime.UtcNow.AddMinutes(10))
            .ToListAsync(stoppingToken);

        foreach (User user in usersNeedingRefresh)
        {
            try
            {
                TokenResponse tokenResponse = await authService.RefreshToken(user.RefreshToken!);

                user.AccessToken = tokenResponse.AccessToken;
                user.RefreshToken = tokenResponse.RefreshToken;
                user.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Refreshed token for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for user {UserId}", user.Id);
            }
        }
    }
}