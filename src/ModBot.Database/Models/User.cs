using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(50)]
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("user_name")] public string Username { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("display_name")] public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    [JsonProperty("timezone")] public string? Timezone { get; set; }
    
    [NotMapped]
    public TimeZoneInfo? TimeZoneInfo => !string.IsNullOrEmpty(Timezone) 
        ? TimeZoneInfo.FindSystemTimeZoneById(Timezone) 
        : null;

    
    [Column("AccessToken")]
    [JsonIgnore]
    [StringLength(2048)]
    // ReSharper disable once InconsistentNaming
    [JsonProperty("access_token")] public string? _accessToken { get; set; }

    [NotMapped]
    [JsonIgnore]
    public string? AccessToken
    {
        get => !string.IsNullOrEmpty(_accessToken)
            ? TokenStore.GetToken(_accessToken)
            : null;
        set => _accessToken = !string.IsNullOrEmpty(value)
            ? TokenStore.EncryptToken(value)
            : null;
    }

    [Column("RefreshToken")]
    [JsonIgnore]
    [StringLength(2048)]
    // ReSharper disable once InconsistentNaming
    [JsonProperty("refresh_token")] public string? _refreshToken { get; set; }

    [NotMapped]
    [JsonIgnore]
    public string? RefreshToken
    {
        get => !string.IsNullOrEmpty(_refreshToken)
            ? TokenStore.GetToken(_refreshToken)
            : null;
        set => _refreshToken = !string.IsNullOrEmpty(value)
            ? TokenStore.EncryptToken(value)
            : null;
    }
    
    [MaxLength(255)]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(2048)]
    [JsonProperty("profile_image_url")] public string ProfileImageUrl { get; set; } = string.Empty;
    
    
    [MaxLength(2048)]
    [JsonProperty("offline_image_url")] public string OfflineImageUrl { get; set; } = string.Empty;
    
    [MaxLength(7)]
    [JsonProperty("offline_image_url")] public string? Color { get; set; }
    
    [JsonProperty("enabled")] public bool Enabled { get; set; }

    [JsonProperty("is_live")] public bool IsLive { get; set; }

    [JsonIgnore]
    [JsonProperty("token_expiry")] public DateTime? TokenExpiry { get; set; }

    [JsonProperty("channels")] public ICollection<Channel> ModeratedChannels { get; set; } = new List<Channel>();
    [JsonProperty("broadcaster")] public ICollection<Channel> BroadcasterChannels { get; set; } = new List<Channel>();

    [JsonProperty("broadcaster_blocked_terms")] public ICollection<BlockedTerm> BroadcasterBlockedTerms { get; set; } = new List<BlockedTerm>();

    [JsonProperty("moderator_blocked_terms")] public ICollection<BlockedTerm> ModeratorBlockedTerms { get; set; } = new List<BlockedTerm>();
}