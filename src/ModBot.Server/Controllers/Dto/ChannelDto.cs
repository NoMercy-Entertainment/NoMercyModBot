using ModBot.Database.Models;
using Newtonsoft.Json;

namespace ModBot.Server.Controllers.Dto;

public record ChannelDto
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("broadcaster_login")] public string BroadcasterLogin { get; set; } = string.Empty;
    [JsonProperty("broadcaster_name")] public string BroadcasterName { get; set; } = string.Empty;
    [JsonProperty("broadcaster_id")] public string BroadcasterId { get; set; } = string.Empty;
    [JsonProperty("moderator_id")] public string ModeratorId { get; set; } = string.Empty;
    [JsonProperty("link")] public Uri Link { get; set; } = null!;
    
    [JsonProperty("broadcaster")] public UserDto Broadcaster { get; set; } = null!;
    // [JsonProperty("moderator")] public UserDto Moderator { get; set; } = null!;
    
    public ChannelDto(Channel channel)
    {
        Id = channel.Id;
        BroadcasterLogin = channel.Broadcaster.Username;
        BroadcasterName = channel.Broadcaster.DisplayName;
        BroadcasterId = channel.BroadcasterId;
        ModeratorId = channel.ModeratorId;
        Link = new($"/channels/{channel.Broadcaster.Username}", UriKind.Relative);
        
        Broadcaster = new(channel.Broadcaster);
        // Moderator = new(channel.Moderator);
    }
}