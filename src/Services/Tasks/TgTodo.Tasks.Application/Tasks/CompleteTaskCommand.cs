using System.Text.Json;
using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.BuildingBlocks.Outbox;
using TgTodo.Contracts.Enums;
using TgTodo.Contracts.Events;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Application.Services;

namespace TgTodo.Tasks.Application.Tasks;

public record CompleteTaskCommand(Guid UserId, Guid TaskId, DateOnly? Date) : IRequest<TaskDto>;

public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly TaskAccessService _access;

    public CompleteTaskCommandHandler(ITaskRepository tasks, TaskAccessService access)
    {
        _tasks = tasks;
        _access = access;
    }

    public async Task<TaskDto> Handle(CompleteTaskCommand request, CancellationToken ct)
    {
        var task = await _tasks.GetTaskByIdAsync(request.TaskId, ct)
            ?? throw new NotFoundException("Task not found.");

        await _access.EnsureCanCompleteAsync(task, request.UserId, ct);

        var dueDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (!RecurrenceSchedule.IsDueOnDate(task, dueDate))
            throw new ConflictException("Задача не запланирована на этот день.");

        var periodKey = PeriodKeyService.GetCurrentPeriodKey(task, dueDate);

        var completionUserId = request.UserId;
        var userFilter = task.CompletionMode == CompletionMode.EachMember ? completionUserId : (Guid?)null;

        if (await _tasks.HasCompletionAsync(task.Id, periodKey, userFilter, ct))
            throw new ConflictException("Task already completed for this period.");

        var completion = Domain.Entities.TaskCompletion.Create(task.Id, completionUserId, periodKey);
        await _tasks.AddCompletionAsync(completion, ct);

        var eventId = Guid.NewGuid();
        var integrationEvent = new TaskCompletedIntegrationEvent(
            eventId,
            task.Id,
            request.UserId,
            task.Scope,
            task.GroupId,
            task.PointsReward,
            periodKey,
            DateTime.UtcNow);

        var outbox = new OutboxMessage
        {
            Id = eventId,
            Type = nameof(TaskCompletedIntegrationEvent),
            Content = JsonSerializer.Serialize(integrationEvent),
            OccurredAt = DateTime.UtcNow
        };
        await _tasks.AddOutboxAsync(outbox, ct);
        await _tasks.SaveChangesAsync(ct);

        return CreateTaskCommandHandler.Map(task, true, periodKey);
    }
}
