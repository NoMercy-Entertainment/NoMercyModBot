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
    public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Text { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(255)]
    [ForeignKey(nameof(Broadcaster))] 
    public string BroadcasterId { get; set; } = string.Empty;

    public User Broadcaster { get; set; } = null!;

    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    public string ModeratorId { get; set; } = string.Empty;

    public User Moderator { get; set; } = null!;
}