using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(ModeratorId), nameof(BroadcasterId), IsUnique = true)]
public class Channel
{
    private Uri _link = null!;
    [MaxLength(255)] [JsonProperty("id")] public int Id { get; set; }

    [MaxLength(255)]
    [JsonProperty("broadcaster_login")]
    public string BroadcasterLogin { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("broadcaster_name")]
    public string BroadcasterName { get; set; } = string.Empty;

    [MaxLength(255)]
    [ForeignKey(nameof(Broadcaster))]
    [JsonProperty("broadcaster_id")]
    public string BroadcasterId { get; set; } = string.Empty;

    [JsonProperty("broadcaster")] public User Broadcaster { get; set; } = null!;

    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    [JsonProperty("moderator_id")]
    public string ModeratorId { get; set; } = string.Empty;

    [JsonProperty("moderator")] public User Moderator { get; set; } = null!;
}