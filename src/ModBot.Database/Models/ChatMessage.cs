using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
// ReSharper disable MemberCanBePrivate.Global

namespace ModBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class ChatMessage: Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(255)] public string Id { get; set; }
    public List<KeyValuePair<string, string>>? BadgeInfo { get; set; }
    public Color? Color { get; set; }
    public List<KeyValuePair<string, string>>? Badges { get; set; }
    public int Bits { get; set; }
    public double BitsInDollars { get; set; }
    public CheerBadge? CheerBadge  { get; set; }
    public EmoteSet? EmoteSet  { get; set; }
    public string? CustomRewardId { get; set; }
    
    private string? _emoteReplacedMessage;

    [NotMapped]
    public string? EmoteReplacedMessage
    {
        get => ReplaceEmotesWithImageTags();
        set => _emoteReplacedMessage = value;
    }

    private string ReplaceEmotesWithImageTags()
    {
        if (string.IsNullOrEmpty(Message) || EmoteSet?.Emotes == null || !EmoteSet.Emotes.Any())
            return Message;

        var stringReplacements = EmoteSet.Emotes
            .Select(emote =>
            {
                string stringToReplace = emote.Name;
                string replacement = $"<img " +
                                     $"src=\"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0\" " +
                                     $"style=\"width:30px;height:30px;transform:translateY(25%);\" " +
                                     $"alt=\"{stringToReplace}\" " +
                                     $"title=\"{stringToReplace}\">";

                return new { stringToReplace, replacement };
            })
            .ToList();

        // Use the same reduction pattern as in TypeScript
        string result = stringReplacements.Aggregate(
            Message,
            (current, replacement) => 
                current.Replace(replacement.stringToReplace, replacement.replacement)
        );

        return result;
    }
    
    public bool IsBroadcaster { get; set; }
    public bool IsFirstMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsMe { get; set; }
    public bool IsModerator { get; set; }
    public bool IsSkippingSubMode { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsVip { get; set; }
    public bool IsStaff { get; set; }
    public bool IsPartner { get; set; }
    public string Message { get; set; }
    public Noisy Noisy { get; set; }
    public int SubscribedMonthCount { get; set; }
    public string TmiSentTs { get; set; }
    
    public string BotUsername { get; set; }
    public string ColorHex { get; set; }
    public string DisplayName { get; set; }
    public bool IsTurbo { get; set; }
    public string Username { get; set; }
    public UserType UserType { get; set; }
    
    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    public string UserId { get; set; } = string.Empty;
    public User Moderator { get; set; } = null!;
    
    public bool IsReturningChatter { get; set; }
    public string? RewardId { get; set; }
    
    public string ChannelId { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    public virtual User Broadcaster { get; set; } = null!;

    public string? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    public virtual ChatMessage? ReplyToMessage { get; set; }

    public virtual ICollection<ChatMessage> Replies { get; set; } = [];




#pragma warning disable CS8618, CS9264
    public ChatMessage()
#pragma warning restore CS8618, CS9264
    {
        
    }

    public ChatMessage(TwitchLib.Client.Models.ChatMessage chatMessage)
    {
        BadgeInfo = chatMessage.BadgeInfo;
        Badges = chatMessage.Badges;
        ChannelId = chatMessage.RoomId;
        Bits = chatMessage.Bits;
        BitsInDollars = chatMessage.BitsInDollars;
        BotUsername = chatMessage.BotUsername;
        CheerBadge = chatMessage.CheerBadge;
        Color = chatMessage.Color;
        ColorHex = chatMessage.ColorHex;
        CustomRewardId = chatMessage.CustomRewardId;
        DisplayName = chatMessage.DisplayName;
        EmoteReplacedMessage = chatMessage.EmoteReplacedMessage;
        EmoteSet = chatMessage.EmoteSet.Emotes.Count > 0
            ? MyEmoteSet.FromTwitchEmoteSet(chatMessage.EmoteSet)
            : null;
        Id = chatMessage.Id;
        IsBroadcaster = chatMessage.IsBroadcaster;
        IsFirstMessage = chatMessage.IsFirstMessage;
        IsHighlighted = chatMessage.IsHighlighted;
        IsMe = chatMessage.IsMe;
        IsModerator = chatMessage.IsModerator;
        IsPartner = chatMessage.IsPartner;
        IsSkippingSubMode = chatMessage.IsSkippingSubMode;
        IsStaff = chatMessage.IsStaff;
        IsSubscriber = chatMessage.IsSubscriber;
        IsTurbo = chatMessage.IsTurbo;
        IsVip = chatMessage.IsVip;
        Message = chatMessage.Message;
        Noisy = chatMessage.Noisy;
        SubscribedMonthCount = chatMessage.SubscribedMonthCount;
        TmiSentTs = chatMessage.TmiSentTs;
        UserId = chatMessage.UserId;
        UserType = chatMessage.UserType;
        Username = chatMessage.Username;
        ReplyToMessageId = chatMessage.ChatReply?.ParentMsgId;
    }
}

public class MyEmoteSet : EmoteSet
{
    [JsonProperty("Emotes")]
    public List<Emote> Emotes { get; set; } = new();

    [JsonProperty("RawEmoteSetString")]
    public string RawEmoteSetString { get; set; } = string.Empty;

    public MyEmoteSet(string rawEmoteSetString, string message) : base(rawEmoteSetString, message)
    {
        RawEmoteSetString = rawEmoteSetString;
    }

    public MyEmoteSet(IEnumerable<Emote> emotes, string emoteSetData) 
        : base(emoteSetData, "") // Use the string constructor instead
    {
        Emotes = emotes.ToList();
        RawEmoteSetString = emoteSetData;
    }

    public MyEmoteSet() : base("", "") { }

    // Add conversion from TwitchLib EmoteSet
    public static MyEmoteSet? FromTwitchEmoteSet(EmoteSet emoteSet)
    {
        List<Emote> myEmotes = emoteSet.Emotes.Select(e => new Emote(
            e.Id,
            e.Name,
            e.StartIndex,
            e.EndIndex,
            $"https://static-cdn.jtvnw.net/emoticons/v1/{e.Id}/1.0"
        )).ToList();

        return new(myEmotes, emoteSet.RawEmoteSetString);
    }
}

public class Emote
{    
    [JsonProperty("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("StartIndex")]
    public int StartIndex { get; set; }

    [JsonProperty("EndIndex")]
    public int EndIndex { get; set; }

    [JsonProperty("ImageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    public Emote(string id, string name, int startIndex, int endIndex, string imageUrl)
    {
        Id = id;
        Name = name;
        StartIndex = startIndex;
        EndIndex = endIndex;
        ImageUrl = imageUrl;
    }

    public Emote() { }
}