using TgTodo.Contracts.Enums;

namespace TgTodo.Contracts.Events;

public record TaskCompletedIntegrationEvent(
    Guid EventId,
    Guid TaskId,
    Guid CompletedByUserId,
    TaskScope Scope,
    Guid? GroupId,
    int PointsReward,
    string PeriodKey,
    DateTime OccurredAt);
