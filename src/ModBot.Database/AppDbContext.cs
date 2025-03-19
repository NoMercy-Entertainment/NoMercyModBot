using Microsoft.EntityFrameworkCore;
using ModBot.Database.Models;

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
            .WithMany(u => u.ModeratedChannels)
            .HasForeignKey(bt => bt.ModeratorId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<BlockedTerm> BlockedTerms { get; set; }
}