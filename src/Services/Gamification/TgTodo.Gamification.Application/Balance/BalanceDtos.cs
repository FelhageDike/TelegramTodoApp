namespace TgTodo.Gamification.Application.Balance;

public record BalanceDto(int PersonalBalance, int? GroupBalance);
public record LedgerEntryDto(int Delta, string Reason, Guid? ReferenceId, DateTime CreatedAt);
