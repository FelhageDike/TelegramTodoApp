using TgTodo.Contracts.Enums;

namespace TgTodo.Bot.Services;

public record GroupDto(Guid Id, string Name, string InviteCode, GroupRole MyRole);

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

public record BalanceDto(int PersonalBalance, int? GroupBalance);

public record LedgerEntryDto(int Delta, string Reason, Guid? ReferenceId, DateTime CreatedAt);

public record HomeDto(
    Guid UserId,
    BalanceDto Balance,
    List<TaskDto> Tasks,
    List<GroupDto> Groups);
