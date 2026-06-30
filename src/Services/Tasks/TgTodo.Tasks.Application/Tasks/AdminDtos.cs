using TgTodo.Contracts.Enums;

namespace TgTodo.Tasks.Application.Tasks;

public record AdminUserTaskStatsDto(Guid UserId, int ActiveTasks, int CompletedTasks);

public record AdminTaskDto(
    Guid Id,
    string Title,
    TaskScope Scope,
    Contracts.Enums.TaskStatus Status,
    RecurrenceType Recurrence,
    int UserCompletionCount);
