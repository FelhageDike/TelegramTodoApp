using Microsoft.EntityFrameworkCore;
using TgTodo.BuildingBlocks.Outbox;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Infrastructure.Persistence;

public class TasksDbContext : DbContext
{
    public TasksDbContext(DbContextOptions<TasksDbContext> options) : base(options) { }

    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TaskCompletion> TaskCompletions => Set<TaskCompletion>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoTask>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<TaskCompletion>(e =>
        {
            e.ToTable("task_completions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TaskId, x.UserId, x.PeriodKey }).IsUnique();
            e.Property(x => x.PeriodKey).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
        });
    }
}
