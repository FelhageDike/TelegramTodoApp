using Microsoft.EntityFrameworkCore;
using TgTodo.Gamification.Domain.Entities;

namespace TgTodo.Gamification.Infrastructure.Persistence;

public class GamificationDbContext : DbContext
{
    public GamificationDbContext(DbContextOptions<GamificationDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PointLedgerEntry> LedgerEntries => Set<PointLedgerEntry>();
    public DbSet<ProcessedIntegrationEvent> ProcessedEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
            e.HasIndex(x => x.GroupId).IsUnique().HasFilter("\"GroupId\" IS NOT NULL");
        });

        modelBuilder.Entity<PointLedgerEntry>(e =>
        {
            e.ToTable("point_ledger");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(64);
        });

        modelBuilder.Entity<ProcessedIntegrationEvent>(e =>
        {
            e.ToTable("processed_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EventId).IsUnique();
        });
    }
}
