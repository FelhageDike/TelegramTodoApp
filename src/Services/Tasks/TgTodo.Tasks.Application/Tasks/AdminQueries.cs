using MediatR;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Application.Abstractions;

namespace TgTodo.Tasks.Application.Tasks;

public record GetAdminTaskStatsQuery : IRequest<IReadOnlyList<AdminUserTaskStatsDto>>;

public class GetAdminTaskStatsQueryHandler : IRequestHandler<GetAdminTaskStatsQuery, IReadOnlyList<AdminUserTaskStatsDto>>
{
    private readonly ITaskRepository _tasks;

    public GetAdminTaskStatsQueryHandler(ITaskRepository tasks) => _tasks = tasks;

    public async Task<IReadOnlyList<AdminUserTaskStatsDto>> Handle(GetAdminTaskStatsQuery request, CancellationToken ct)
    {
        var activeCounts = await _tasks.GetActiveTaskCountsByUserAsync(ct);
        var completedCounts = await _tasks.GetCompletedTaskCountsByUserAsync(ct);

        var userIds = activeCounts.Keys.Union(completedCounts.Keys).ToHashSet();
        return userIds
            .Select(userId => new AdminUserTaskStatsDto(
                userId,
                activeCounts.GetValueOrDefault(userId),
                completedCounts.GetValueOrDefault(userId)))
            .ToList();
    }
}

public record GetAdminUserTasksQuery(Guid UserId) : IRequest<IReadOnlyList<AdminTaskDto>>;

public class GetAdminUserTasksQueryHandler : IRequestHandler<GetAdminUserTasksQuery, IReadOnlyList<AdminTaskDto>>
{
    private readonly ITaskRepository _tasks;

    public GetAdminUserTasksQueryHandler(ITaskRepository tasks) => _tasks = tasks;

    public async Task<IReadOnlyList<AdminTaskDto>> Handle(GetAdminUserTasksQuery request, CancellationToken ct)
    {
        var userTasks = await _tasks.GetTasksForUserAdminAsync(request.UserId, ct);
        var completionCounts = await _tasks.GetUserCompletionCountsByTaskAsync(request.UserId, ct);

        return userTasks
            .Select(t => new AdminTaskDto(
                t.Id,
                t.Title,
                t.Scope,
                t.Status,
                t.Recurrence,
                completionCounts.GetValueOrDefault(t.Id)))
            .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
