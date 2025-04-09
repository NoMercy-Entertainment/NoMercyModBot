using ModBot.Database;
using ModBot.Server.Services.Twitch;

namespace ModBot.Server.Services.Commands;

public class CommandsCommand : ACommand
{
    public override string Name => "commands";

    public override string Description => "Show all commands";

    private readonly TwitchCommandsService _service;
    private readonly AppDbContext _db;

    public CommandsCommand(TwitchCommandsService service, AppDbContext db) : base()
    {
        _service = service;
        _db = db;
    }

    // public override async Task<ProcessedChatMessage> ExecuteAsync(ProcessedChatMessage chatMessage, TwitchChatMessagesService chat)
    // {
    //     List<string> commandNames = [];
    //     commandNames.AddRange(_service.Commands.Where(c => c.Value.IsAuthorizedToExecute(chatMessage.Original)).Select(c => c.Value.Name));
    //     commandNames.AddRange(_service.ExternalCommands.Select(c => c.Key));
    //     commandNames.AddRange(await _db.TextCommands.Select(c => c.Name).ToArrayAsync());
    //
    //     return chatMessage
    //         .WithoutMessage()
    //         .WithReply($"@{chatMessage.Original.DisplayName}, "  + string.Join(", ", commandNames.OrderBy(c => c).Select(c => $"!{c}")));
    // }
}
