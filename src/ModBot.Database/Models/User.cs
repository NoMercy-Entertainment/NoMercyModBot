using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.None), MaxLength(50)]
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("user_name")] public string Username { get; set; } = string.Empty;

    [MaxLength(255)]
    [JsonProperty("display_name")] public string DisplayName { get; set; } = string.Empty;

    [Column("AccessToken"), JsonIgnore, StringLength(2048)]
    // ReSharper disable once InconsistentNaming
    [JsonProperty("access_token")] public string _accessToken { get; set; } = string.Empty;

    [NotMapped, JsonIgnore]
    public string AccessToken
    {
        get => (_accessToken != string.Empty
            ? TokenStore.GetToken(_accessToken)
            : string.Empty) ?? string.Empty;
        init => _accessToken = TokenStore.EncryptToken(value);
    }
    
    [Column("RefreshToken"), JsonIgnore, StringLength(2048)]
    // ReSharper disable once InconsistentNaming
    [JsonProperty("refresh_token")] public string _refreshToken { get; set; } = string.Empty;

    [NotMapped, JsonIgnore]
    public string RefreshToken
    {
        get => (_refreshToken != string.Empty
            ? TokenStore.GetToken(_refreshToken)
            : string.Empty) ?? string.Empty;
        init => _refreshToken = TokenStore.EncryptToken(value);
    }
    
    [MaxLength(2048), JsonIgnore]
    [JsonProperty("profile_image_url")] public string ProfileImageUrl { get; set; } = string.Empty;

    [JsonIgnore]
    [JsonProperty("token_expiry")] public DateTime TokenExpiry { get; set; }

    [JsonProperty("channels")] public ICollection<Channel>? ModeratedChannels { get; set; }
    [JsonProperty("broadcaster")] public ICollection<Channel>? BroadcasterChannels { get; set; }

    [JsonProperty("broadcaster_blocked_terms")] public ICollection<BlockedTerm>? BroadcasterBlockedTerms { get; set; }

    [JsonProperty("moderator_blocked_terms")] public ICollection<BlockedTerm>? ModeratorBlockedTerms { get; set; }
}