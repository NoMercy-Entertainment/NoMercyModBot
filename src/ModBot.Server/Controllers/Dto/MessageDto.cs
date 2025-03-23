using ModBot.Database.Models;
using Newtonsoft.Json;
using TwitchLib.Client.Models;
using ChatMessage = ModBot.Database.Models.ChatMessage;

namespace ModBot.Server.Controllers.Dto;
public class MessageDto
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("roomId")] public string RoomId { get; set; }
    [JsonProperty("userId")] public string UserId { get; set; }
    [JsonProperty("username")] public string Username { get; set; }
    [JsonProperty("displayName")] public string DisplayName { get; set; }
    [JsonProperty("message")] public string Message { get; set; }
    [JsonProperty("emoteSet")] public MyEmoteSet EmoteSet { get; set; }
    [JsonProperty("emoteReplacedMessage")] public string? EmoteReplacedMessage { get; set; }
    [JsonProperty("colorHex")] public string ColorHex { get; set; }
    [JsonProperty("timestamp")] public string TmiSentTs { get; set; }
    [JsonProperty("bits")] public int Bits { get; set; }
    [JsonProperty("bitsInDollars")] public double BitsInDollars { get; set; }
    [JsonProperty("channelId")] public string ChannelId { get; set; }
    [JsonProperty("isFirstMessage")] public bool IsFirstMessage { get; set; }
    [JsonProperty("isHighlighted")] public bool IsHighlighted { get; set; }
    [JsonProperty("isMe")] public bool IsMe { get; set; }
    [JsonProperty("isReturningChatter")] public bool IsReturningChatter { get; set; }
    [JsonProperty("isSubscriber")] public bool IsSubscriber { get; set; }
    [JsonProperty("rewardId")] public string? RewardId { get; set; }
    [JsonProperty("subscribedMonthCount")] public int SubscribedMonthCount { get; set; }
    [JsonProperty("badgeInfo")] public List<KeyValuePair<string, string>>? BadgeInfo { get; set; }
    [JsonProperty("badges")] public List<KeyValuePair<string, string>>? Badges { get; set; }
    [JsonProperty("cheerBadge")] public CheerBadge? CheerBadge { get; set; }
    [JsonProperty("reply")] public MessageDto? Reply { get; set; }
    [JsonProperty("userType")] public Dictionary<string, bool> UserType { get; set; }
    [JsonProperty("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonProperty("updatedAt")] public DateTime UpdatedAt { get; set; }

    public MessageDto(ChatMessage message)
    {
        Id = message.Id;
        RoomId = message.ChannelId;
        UserId = message.UserId;
        Username = message.Username;
        DisplayName = message.DisplayName;
        Message = message.Message;
        EmoteSet = (MyEmoteSet?)message.EmoteSet ?? new();
        EmoteReplacedMessage = message.EmoteReplacedMessage;
        ColorHex = message.ColorHex;
        TmiSentTs = message.TmiSentTs;
        Bits = message.Bits;
        BitsInDollars = message.BitsInDollars;
        ChannelId = message.ChannelId;
        IsFirstMessage = message.IsFirstMessage;
        IsHighlighted = message.IsHighlighted;
        IsMe = message.IsMe;
        IsReturningChatter = message.IsReturningChatter;
        IsSubscriber = message.IsSubscriber;
        RewardId = message.RewardId;
        SubscribedMonthCount = message.SubscribedMonthCount;
        BadgeInfo = message.BadgeInfo;
        Badges = message.Badges;
        CheerBadge = message.CheerBadge;
        CreatedAt = message.CreatedAt;
        UpdatedAt = message.UpdatedAt;

        UserType = new()
        {
            { "broadcaster", message.IsBroadcaster },
            { "moderator", message.IsModerator },
            { "subscriber", message.IsSubscriber },
            { "vip", message.IsVip },
            { "staff", message.IsStaff },
            { "partner", message.IsPartner }
        };

        if (message.ReplyToMessage?.Id != null)
        {
            Reply = new(message.ReplyToMessage);
        }
    }
    
    public class EmoteData
    {
        public List<EmoteItem> Emotes { get; set; } = [];
    }

    public class EmoteItem(string id, string name, int startIndex, int endIndex)
    {
        public string Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int StartIndex { get; set; } = startIndex;
        public int EndIndex { get; set; } = endIndex;
    }
}