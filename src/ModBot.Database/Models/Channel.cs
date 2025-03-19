using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(ModeratorId), nameof(BroadcasterId), IsUnique = true)]
public class Channel
{
    [MaxLength(255)] [JsonProperty("id")] public int Id { get; set; }

    [MaxLength(255)]
    [JsonProperty("broadcaster_login")]
    public string BroadcasterLogin { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("broadcaster_name")]
    public string BroadcasterName { get; set; } = string.Empty;

    [JsonProperty("enabled")] public bool Enabled { get; set; }

    [MaxLength(255)]
    [ForeignKey(nameof(Broadcaster))]
    [JsonProperty("broadcaster_id")]
    public string? BroadcasterId { get; set; }

    [JsonProperty("broadcaster")] public User? Broadcaster { get; set; }

    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    [JsonProperty("moderator_id")]
    public string? ModeratorId { get; set; }

    [JsonProperty("moderator")] public User? Moderator { get; set; }
}