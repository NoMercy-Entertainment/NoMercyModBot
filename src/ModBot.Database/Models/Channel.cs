using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(ModeratorId), nameof(BroadcasterId), IsUnique = true)]
public class Channel
{
    [MaxLength(255)] public int Id { get; set; }

    [MaxLength(255)]
    [ForeignKey(nameof(Broadcaster))]
    public string BroadcasterId { get; set; } = string.Empty;

    public User Broadcaster { get; set; } = null!;

    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    public string ModeratorId { get; set; } = string.Empty;

    public User Moderator { get; set; } = null!;
    
    public virtual ICollection<ChatPresence> UsersInChat { get; set; } = [];
}