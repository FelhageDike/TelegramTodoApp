using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Application.Services;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Tasks;

public record CreateTaskCommand(
    Guid UserId,
    TaskScope Scope,
    string Title,
    int PointsReward,
    RecurrenceType Recurrence,
    int? Weekday,
    int? DayOfMonth,
    int? IntervalDays,
    PersonalTaskVisibility PersonalVisibility,
    CompletionMode CompletionMode,
    Guid? GroupId,
    Guid? AssignedToUserId,
    Guid? CategoryId,
    Guid? VisibilityGroupId,
    DateOnly? StartDate) : IRequest<TaskDto>;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IGroupsClient _groups;

    public CreateTaskCommandHandler(ITaskRepository tasks, IGroupsClient groups)
    {
        _tasks = tasks;
        _groups = groups;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Название задачи не может быть пустым.");

        if (request.Scope == TaskScope.Group)
        {
            if (request.GroupId is null)
                throw new ConflictException("GroupId is required for group tasks.");
            if (!await _groups.IsMemberAsync(request.GroupId.Value, request.UserId, ct))
                throw new ForbiddenException("Not a group member.");
        }

        RecurrenceSchedule.Validate(request.Recurrence, request.Weekday, request.DayOfMonth, request.IntervalDays);

        var groupIdForPersonal = request.Scope == TaskScope.Personal ? request.VisibilityGroupId : request.GroupId;
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var task = TodoTask.Create(
            request.Scope,
            request.UserId,
            request.UserId,
            request.Title.Trim(),
            request.PointsReward,
            request.Recurrence,
            request.PersonalVisibility,
            request.CompletionMode,
            request.Scope == TaskScope.Group ? request.GroupId : groupIdForPersonal,
            request.AssignedToUserId,
            request.CategoryId,
            request.Recurrence == RecurrenceType.Weekly ? request.Weekday : null,
            request.Recurrence == RecurrenceType.Monthly ? request.DayOfMonth : null,
            request.Recurrence == RecurrenceType.EveryNDays ? request.IntervalDays : null,
            startDate);

        await _tasks.AddTaskAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);

        var periodKey = PeriodKeyService.GetCurrentPeriodKey(task, startDate);
        return Map(task, false, periodKey);
    }

    internal static TaskDto Map(TodoTask t, bool completed, string _) => new(
        t.Id, t.Scope, t.GroupId, t.Title, t.PointsReward, t.Recurrence,
        t.PersonalVisibility, t.CategoryId, t.AssignedToUserId, t.CreatedByUserId, completed);
}
