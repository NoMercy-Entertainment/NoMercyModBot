using ModBot.Database.Models;

namespace ModBot.Server.Services.Bot.Model;

public class BotInitializationJob
{
    public User User { get; }
    
    public TaskCompletionSource<bool> CompletionSource { get; }

    public BotInitializationJob(User user)
    {
        User = user;
        CompletionSource = new();
    }
}