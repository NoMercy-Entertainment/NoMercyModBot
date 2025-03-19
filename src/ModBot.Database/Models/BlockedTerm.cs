using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class BlockedTerm : Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(255)]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("expires_at")] public DateTime? ExpiresAt { get; set; }

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