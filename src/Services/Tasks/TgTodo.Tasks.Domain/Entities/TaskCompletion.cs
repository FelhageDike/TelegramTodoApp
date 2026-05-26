using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Tasks.Domain.Entities;

public class TaskCompletion : Entity
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public string PeriodKey { get; private set; } = string.Empty;
    public DateTime CompletedAt { get; private set; } = DateTime.UtcNow;

    private TaskCompletion() { }

    public static TaskCompletion Create(Guid taskId, Guid userId, string periodKey) =>
        new() { TaskId = taskId, UserId = userId, PeriodKey = periodKey };
}
