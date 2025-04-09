using ModBot.Database;
using ModBot.Server.Services.Commands;

namespace ModBot.Server.Services.Twitch;

public class TwitchCommandsService
{
    private readonly IServiceScope _scope;
    private readonly AppDbContext _db;
    private readonly ILogger<TwitchCommandsService> _logger;

    private readonly Dictionary<string, ACommand> _commands = new();
    public IReadOnlyDictionary<string, ACommand> Commands => _commands;

    private readonly Dictionary<string, string> _externalCommands = new();
    public IReadOnlyDictionary<string, string> ExternalCommands => _externalCommands;

    public TwitchCommandsService(IServiceScopeFactory serviceScopeFactory, ILogger<TwitchCommandsService> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;

        RegisterCommand(new PingCommand());
        // RegisterCommand(new DiceCommand());
        RegisterCommand(new CommandsCommand(this, _db));
        RegisterCommand(new HelpCommand(this, _db));
        RegisterCommand(new AddCommandCommand(this));
        // RegisterCommand(new RemoveCommandCommand(this));
        // RegisterCommand(new JsCommand(jsEngines));
        // RegisterCommand(new GetVoicesCommand(tts));
        // RegisterCommand(new SetVoiceCommand(_db, tts));
        // RegisterCommand(new LlamaResetCommand(llama));
        // RegisterCommand(new LlamaSetLoreCommand(_db));
        // RegisterCommand(new SongCommand(youtubeHub));

        _externalCommands.Add("drop", "Drop from the sky!");
    }
    
    private void RegisterCommand(ACommand command)
    {
        _commands[command.Name] = command;
    }
}