using ModBot.Database.Models;

namespace ModBot.Server.Services.Bot;

public interface IBotManager
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task InitializeUserBots(User user);
}