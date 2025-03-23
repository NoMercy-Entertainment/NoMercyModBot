using System.Drawing;
using Microsoft.EntityFrameworkCore;
using ModBot.Database.Models;
using Newtonsoft.Json;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using ChatMessage = ModBot.Database.Models.ChatMessage;

namespace ModBot.Database;

public class AppDbContext : DbContext
{
    private static readonly string AppDataPath =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static readonly string DatabasePath = Path.Combine(AppDataPath, "NoMercyModBot", "AppDbContext.db");

    public AppDbContext()
    {
        string dbFolder = Path.Combine(AppDataPath, "NoMercyModBot");
        if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.UseSqlite($"Data Source={DatabasePath}; Pooling=True",
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<string>()
            .HaveMaxLength(256);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.Name is "CreatedAt" or "UpdatedAt")
            .ToList()
            .ForEach(p => p.SetDefaultValueSql("CURRENT_TIMESTAMP"));

        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) && p.IsNullable)
            .ToList()
            .ForEach(p => p.SetDefaultValue(null));

        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .ToList()
            .ForEach(p => p.DeleteBehavior = DeleteBehavior.Cascade);
        
        // Make sure to encrypt and decrypt the access and refresh tokens
        modelBuilder.Entity<User>()
            .Property(e => e.AccessToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));
        
        modelBuilder.Entity<User>()
            .Property(e => e.RefreshToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));

        modelBuilder.Entity<BlockedTerm>()
            .HasOne(bt => bt.Broadcaster)
            .WithMany(u => u.BroadcasterBlockedTerms)
            .HasForeignKey(bt => bt.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlockedTerm>()
            .HasOne(bt => bt.Moderator)
            .WithMany(u => u.ModeratorBlockedTerms)
            .HasForeignKey(bt => bt.ModeratorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Channel>()
            .HasOne(bt => bt.Broadcaster)
            .WithMany(u => u.BroadcasterChannels)
            .HasForeignKey(bt => bt.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Channel>()
            .HasOne(bt => bt.Moderator)
            .WithMany(u => u.ModeratorChannels)
            .HasForeignKey(bt => bt.ModeratorId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.ReplyToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Broadcaster)
                .WithMany(u => u.BroadcasterChatMessages)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.BadgeInfo)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(v) ?? new List<KeyValuePair<string, string>>());


        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.Color)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Color>(v));
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.Badges)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(v) ?? new List<KeyValuePair<string, string>>());

        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.CheerBadge)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<CheerBadge>(v));
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.EmoteSet)
            .HasConversion(
                v => JsonConvert.SerializeObject(v, new JsonSerializerSettings 
                { 
                    TypeNameHandling = TypeNameHandling.All 
                }),
                v => JsonConvert.DeserializeObject<MyEmoteSet>(v, new JsonSerializerSettings 
                { 
                    TypeNameHandling = TypeNameHandling.All 
                }) ?? new MyEmoteSet());
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.UserType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UserType>(v));

    }

    public DbSet<User> Users { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<BlockedTerm> BlockedTerms { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatPresence> ChatPresences { get; set; }
}