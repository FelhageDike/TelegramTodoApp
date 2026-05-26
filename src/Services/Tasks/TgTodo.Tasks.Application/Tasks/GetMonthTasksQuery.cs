using MediatR;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Application.Services;

namespace TgTodo.Tasks.Application.Tasks;

public record GetMonthTasksQuery(Guid UserId, Guid? GroupId, int Year, int Month) : IRequest<MonthTasksDto>;

public record DayTasksDto(DateOnly Date, IReadOnlyList<TaskDto> Tasks);

public record MonthTasksDto(int Year, int Month, IReadOnlyList<DayTasksDto> Days);

public class GetMonthTasksQueryHandler : IRequestHandler<GetMonthTasksQuery, MonthTasksDto>
{
    private readonly ITaskRepository _tasks;
    private readonly TaskAccessService _access;

    public GetMonthTasksQueryHandler(ITaskRepository tasks, TaskAccessService access)
    {
        _tasks = tasks;
        _access = access;
    }

    public async Task<MonthTasksDto> Handle(GetMonthTasksQuery request, CancellationToken ct)
    {
        var year = request.Year;
        var month = request.Month is >= 1 and <= 12 ? request.Month : DateTime.UtcNow.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        var all = await _tasks.GetTasksAsync(request.UserId, request.GroupId, monthStart, ct);
        var buckets = new Dictionary<DateOnly, List<TaskDto>>();

        for (var day = 1; day <= daysInMonth; day++)
            buckets[new DateOnly(year, month, day)] = [];

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

            for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
            {
                if (!RecurrenceSchedule.IsDueOnDate(task, d))
                    continue;

                var periodKey = PeriodKeyService.GetCurrentPeriodKey(task, d);
                var userFilter = task.CompletionMode == Contracts.Enums.CompletionMode.EachMember
                    ? request.UserId
                    : (Guid?)null;
                var completed = await _tasks.HasCompletionAsync(task.Id, periodKey, userFilter, ct);
                buckets[d].Add(CreateTaskCommandHandler.Map(task, completed, periodKey));
            }
        }

        var days = buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new DayTasksDto(kv.Key, kv.Value))
            .ToList();

        return new MonthTasksDto(year, month, days);
    }
}
