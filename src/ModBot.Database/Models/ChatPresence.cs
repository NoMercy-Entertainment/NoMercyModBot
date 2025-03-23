using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(ChannelId), nameof(UserId), IsUnique = true)]
public class ChatPresence
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [MaxLength(50)]
    public int Id { get; set; }
    
    public bool IsPresent { get; set; }
    
    public string ChannelId { get; set; } = null!;
    public virtual User Channel { get; set; } = null!;

    [MaxLength(50)] public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;
    
}