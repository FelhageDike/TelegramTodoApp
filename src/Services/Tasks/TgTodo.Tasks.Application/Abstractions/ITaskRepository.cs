using TgTodo.BuildingBlocks.Outbox;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Abstractions;

public interface ITaskRepository
{
    Task<TodoTask?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TodoTask>> GetTasksAsync(Guid userId, Guid? groupId, DateOnly date, CancellationToken ct = default);
    Task<bool> HasCompletionAsync(Guid taskId, string periodKey, Guid? userId, CancellationToken ct = default);
    Task AddTaskAsync(TodoTask task, CancellationToken ct = default);
    Task AddCompletionAsync(TaskCompletion completion, CancellationToken ct = default);
    Task AddOutboxAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(Guid userId, Guid? groupId, CancellationToken ct = default);
    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);
    Task AddCategoryAsync(Category category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
