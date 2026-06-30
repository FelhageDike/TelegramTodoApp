using Microsoft.EntityFrameworkCore;
using TgTodo.BuildingBlocks.Outbox;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Domain.Entities;
using TgTodo.Tasks.Infrastructure.Persistence;

namespace TgTodo.Tasks.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TasksDbContext _db;

    public TaskRepository(TasksDbContext db) => _db = db;

    public Task<TodoTask?> GetTaskByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TodoTask>> GetTasksAsync(Guid userId, Guid? groupId, DateOnly date, CancellationToken ct = default)
    {
        if (groupId.HasValue)
        {
            var gid = groupId.Value;
            return await _db.Tasks
                .Where(t =>
                    (t.Scope == TaskScope.Group && t.GroupId == gid) ||
                    (t.Scope == TaskScope.Personal && t.GroupId == gid))
                .ToListAsync(ct);
        }

        return await _db.Tasks
            .Where(t =>
                t.Scope == TaskScope.Personal &&
                (t.OwnerUserId == userId || t.CreatedByUserId == userId || t.AssignedToUserId == userId))
            .ToListAsync(ct);
    }

    public Task<bool> HasCompletionAsync(Guid taskId, string periodKey, Guid? userId, CancellationToken ct = default)
    {
        var query = _db.TaskCompletions.Where(c => c.TaskId == taskId && c.PeriodKey == periodKey);
        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId);
        return query.AnyAsync(ct);
    }

    public async Task AddTaskAsync(TodoTask task, CancellationToken ct = default) =>
        await _db.Tasks.AddAsync(task, ct);

    public async Task AddCompletionAsync(TaskCompletion completion, CancellationToken ct = default) =>
        await _db.TaskCompletions.AddAsync(completion, ct);

    public async Task AddOutboxAsync(OutboxMessage message, CancellationToken ct = default) =>
        await _db.OutboxMessages.AddAsync(message, ct);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(Guid userId, Guid? groupId, CancellationToken ct = default)
    {
        if (groupId.HasValue)
            return await _db.Categories.Where(c => c.GroupId == groupId).ToListAsync(ct);

        return await _db.Categories.Where(c => c.UserId == userId).ToListAsync(ct);
    }

    public Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddCategoryAsync(Category category, CancellationToken ct = default) =>
        await _db.Categories.AddAsync(category, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetActiveTaskCountsByUserAsync(CancellationToken ct = default)
    {
        var pendingTasks = await _db.Tasks
            .Where(t => t.Status == Contracts.Enums.TaskStatus.Pending)
            .Select(t => new { t.OwnerUserId, t.CreatedByUserId, t.AssignedToUserId })
            .ToListAsync(ct);

        var counts = new Dictionary<Guid, int>();
        foreach (var task in pendingTasks)
        {
            var userIds = new HashSet<Guid> { task.OwnerUserId, task.CreatedByUserId };
            if (task.AssignedToUserId.HasValue)
                userIds.Add(task.AssignedToUserId.Value);

            foreach (var userId in userIds)
                counts[userId] = counts.GetValueOrDefault(userId) + 1;
        }

        return counts;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCompletedTaskCountsByUserAsync(CancellationToken ct = default) =>
        await _db.TaskCompletions
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

    public async Task<IReadOnlyList<TodoTask>> GetTasksForUserAdminAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Tasks
            .Where(t =>
                t.OwnerUserId == userId ||
                t.CreatedByUserId == userId ||
                t.AssignedToUserId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCompletionCountsByTaskAsync(
        Guid userId,
        CancellationToken ct = default) =>
        await _db.TaskCompletions
            .Where(c => c.UserId == userId)
            .GroupBy(c => c.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
