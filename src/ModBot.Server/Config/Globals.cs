using ModBot.Database;
using TwitchLib.Api.Core.Enums;

namespace ModBot.Server.Config;

public static class Globals
{
    static Globals()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        TwitchClientId = configuration["TWITCH_CLIENT_ID"] ??
                         throw new InvalidOperationException("Client ID not configured.");
        ClientSecret = configuration["TWITCH_CLIENT_SECRET"] ??
                       throw new InvalidOperationException("Client Secret not configured.");
    }

    public static string TwitchClientId { get; private set; }
    public static string ClientSecret { get; private set; }
    
    public static string? TwitchBotId { get; set; } = "104534444";

    public static readonly string[] Scopes =
    [
        
        "channel:read:subscriptions",
        "chat:edit",
        "chat:read",
        "moderation:read",
        "moderator:manage:announcements",
        "moderator:manage:banned_users",
        "moderator:manage:blocked_terms",
        "moderator:manage:chat_messages",
        "moderator:manage:chat_settings",
        "moderator:manage:shoutouts",
        "moderator:manage:warnings",
        "moderator:read:chat_messages",
        "moderator:read:chat_settings",
        "moderator:read:chatters",
        "moderator:read:followers",
        "moderator:read:shoutouts",
        "moderator:read:warnings",
        "user:read:moderated_channels",
        "user:read:subscriptions",
        "user:write:chat"
    ];

    public static List<AuthScopes> TwitchScopes { get; set; } =
    [
        AuthScopes.Chat_Edit,
        AuthScopes.Chat_Read,
        AuthScopes.Helix_Moderation_Read,
        AuthScopes.Helix_Moderator_Manage_Banned_Users,
        AuthScopes.Helix_Moderator_Manage_Blocked_Terms,
        AuthScopes.Helix_moderator_Manage_Chat_Messages,
        AuthScopes.Helix_Moderator_Manage_Chat_Settings,
        AuthScopes.Helix_Moderator_Read_Chat_Settings,
        AuthScopes.Helix_Moderator_Read_Chatters
    ];

    public static string RedirectUri => "https://modbot.nomercy.tv/auth/callback";
    public static string EventSubCallbackUri => "https://modbot.nomercy.tv/api/eventsub";
    public static string TwitchApiUrl => "https://api.twitch.tv/helix";
    public static string TwitchAuthUrl => "https://id.twitch.tv/oauth2";
}