using ModBot.Database;
using ModBot.Server.Services.Twitch;

namespace ModBot.Server.Services.Commands;

public class HelpCommand : ACommand
{
    public override string Name => "help";

    public override string Description => "Show description of the command. Use !help <commandName>";

    private readonly TwitchCommandsService _service;
    private readonly AppDbContext _db;

    public HelpCommand(TwitchCommandsService service, AppDbContext db) : base()
    {
        _service = service;
        _db = db;
    }

    // public override async Task<ProcessedChatMessage> ExecuteAsync(ProcessedChatMessage chatMessage, TwitchChatMessagesService chat)
    // {
    //     if(chatMessage.CommandArgs.Length == 0)
    //         return chatMessage.WithoutMessage().WithReply($"@{chatMessage.Original.DisplayName}, specify comamnd name to see description");
    //     else
    //     {
    //         string commandName = chatMessage.CommandArgs.First();
    //         if(_service.Commands.ContainsKey(commandName))
    //         {
    //             if(_service.Commands[commandName].IsAuthorizedToExecute(chatMessage.Original))
    //                 return chatMessage.WithoutMessage().WithReply($"@{chatMessage.Original.DisplayName}, {_service.Commands[commandName].Description}");
    //             else return chatMessage.WithReply($"@{chatMessage.Original.DisplayName}, you not authorized to execute this command");
    //         }
    //         else if(_service.ExternalCommands.TryGetValue(commandName, out string? command))
    //             return chatMessage.WithoutMessage().WithReply($"@{chatMessage.Original.DisplayName}, {command}");
    //         else
    //         {
    //             TextCommandModel? model = await _db.TextCommands.FirstOrDefaultAsync(c => c.Name.ToLower() == commandName.ToLower());
    //             if(model is not null) return chatMessage.WithoutMessage().WithReply($"@{chatMessage.Original.DisplayName}, command with useful information ;)");
    //             else return chatMessage.WithoutMessage().WithReply($"@{chatMessage.Original.DisplayName}, {commandName} not found");
    //         }
    //     }
    // }
}