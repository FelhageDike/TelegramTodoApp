using TgTodo.Contracts.Enums;

namespace TgTodo.Tasks.Application.Tasks;

public record TaskDto(
    Guid Id,
    TaskScope Scope,
    Guid? GroupId,
    string Title,
    int PointsReward,
    RecurrenceType Recurrence,
    PersonalTaskVisibility PersonalVisibility,
    Guid? CategoryId,
    Guid? AssignedToUserId,
    Guid CreatedByUserId,
    bool IsCompletedForPeriod);

public record CategoryDto(Guid Id, string Name, string? Emoji, Guid? GroupId);
