using MediatR;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Application.Services;

namespace TgTodo.Tasks.Application.Tasks;

public record GetTasksQuery(Guid UserId, Guid? GroupId, DateOnly? Date) : IRequest<IReadOnlyList<TaskDto>>;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, IReadOnlyList<TaskDto>>
{
    private readonly ITaskRepository _tasks;
    private readonly TaskAccessService _access;

    public GetTasksQueryHandler(ITaskRepository tasks, TaskAccessService access)
    {
        _tasks = tasks;
        _access = access;
    }

    public async Task<IReadOnlyList<TaskDto>> Handle(GetTasksQuery request, CancellationToken ct)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _tasks.GetTasksAsync(request.UserId, request.GroupId, date, ct);
        var result = new List<TaskDto>();

        foreach (var task in all)
        {
            try
            {
                await _access.EnsureCanViewAsync(task, request.UserId, ct);
            }
            catch
            {
                continue;
            }

            if (!RecurrenceSchedule.IsDueOnDate(task, date))
                continue;

            var periodKey = PeriodKeyService.GetCurrentPeriodKey(task, date);
            var userFilter = task.CompletionMode == Contracts.Enums.CompletionMode.EachMember
                ? request.UserId
                : (Guid?)null;
            var completed = await _tasks.HasCompletionAsync(task.Id, periodKey, userFilter, ct);

            result.Add(CreateTaskCommandHandler.Map(task, completed, periodKey));
        }

        return result;
    }
}
