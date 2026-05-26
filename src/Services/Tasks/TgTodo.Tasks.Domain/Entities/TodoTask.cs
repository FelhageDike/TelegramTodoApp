using TgTodo.BuildingBlocks.Domain;
using TgTodo.Contracts.Enums;
using TaskItemStatus = TgTodo.Contracts.Enums.TaskStatus;

namespace TgTodo.Tasks.Domain.Entities;

public class TodoTask : AuditableEntity
{
    public TaskScope Scope { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public PersonalTaskVisibility PersonalVisibility { get; private set; }
    public CompletionMode CompletionMode { get; private set; }
    public RecurrenceType Recurrence { get; private set; }
    public int? Weekday { get; private set; }
    public int? DayOfMonth { get; private set; }
    public int? IntervalDays { get; private set; }
    public DateOnly RecurrenceStartDate { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int PointsReward { get; private set; }
    public int PenaltyPoints { get; private set; }
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Pending;

    private TodoTask() { }

    public static TodoTask Create(
        TaskScope scope,
        Guid ownerUserId,
        Guid createdByUserId,
        string title,
        int pointsReward,
        RecurrenceType recurrence,
        PersonalTaskVisibility personalVisibility,
        CompletionMode completionMode,
        Guid? groupId = null,
        Guid? assignedToUserId = null,
        Guid? categoryId = null,
        int? weekday = null,
        int? dayOfMonth = null,
        int? intervalDays = null,
        DateOnly? recurrenceStartDate = null)
    {
        var start = recurrenceStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return new TodoTask
        {
            Scope = scope,
            GroupId = groupId,
            OwnerUserId = ownerUserId,
            CreatedByUserId = createdByUserId,
            AssignedToUserId = assignedToUserId,
            PersonalVisibility = personalVisibility,
            CompletionMode = completionMode,
            Recurrence = recurrence,
            Weekday = weekday,
            DayOfMonth = dayOfMonth,
            IntervalDays = intervalDays,
            RecurrenceStartDate = start,
            CategoryId = categoryId,
            Title = title.Trim(),
            PointsReward = Math.Max(0, pointsReward),
            PenaltyPoints = 0
        };
    }
}
