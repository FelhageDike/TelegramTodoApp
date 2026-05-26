using TgTodo.Contracts.Enums;
using TgTodo.Gamification.Domain.Entities;

namespace TgTodo.Gamification.Application.Abstractions;

public interface IGamificationRepository
{
    Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkEventProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task<Account?> GetPersonalAccountAsync(Guid userId, CancellationToken ct = default);
    Task<Account?> GetGroupAccountAsync(Guid groupId, CancellationToken ct = default);
    Task AddAccountAsync(Account account, CancellationToken ct = default);
    Task AddLedgerEntryAsync(PointLedgerEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<PointLedgerEntry>> GetLedgerAsync(Guid accountId, int take, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
