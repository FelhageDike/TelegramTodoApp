using Microsoft.EntityFrameworkCore;
using TgTodo.Groups.Domain.Entities;

namespace TgTodo.Groups.Infrastructure.Persistence;

public class GroupsDbContext : DbContext
{
    public GroupsDbContext(DbContextOptions<GroupsDbContext> options) : base(options) { }

    public DbSet<FamilyGroup> Groups => Set<FamilyGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FamilyGroup>(e =>
        {
            e.ToTable("groups");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.InviteCode).HasMaxLength(8).IsRequired();
        });

        modelBuilder.Entity<GroupMember>(e =>
        {
            e.ToTable("group_members");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId);
        });
    }
}
