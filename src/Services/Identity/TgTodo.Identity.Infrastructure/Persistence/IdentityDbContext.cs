using Microsoft.EntityFrameworkCore;
using TgTodo.Identity.Domain.Entities;

namespace TgTodo.Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TelegramId).IsUnique();
            e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
        });
    }
}
