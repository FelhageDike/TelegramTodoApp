using Microsoft.EntityFrameworkCore;
using TgTodo.Gamification.Application.Abstractions;
using TgTodo.Gamification.Domain.Entities;
using TgTodo.Gamification.Infrastructure.Persistence;

namespace TgTodo.Gamification.Infrastructure.Repositories;

public class GamificationRepository : IGamificationRepository
{
    private readonly GamificationDbContext _db;

    public GamificationRepository(GamificationDbContext db) => _db = db;

    public Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken ct = default) =>
        _db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkEventProcessedAsync(Guid eventId, CancellationToken ct = default) =>
        await _db.ProcessedEvents.AddAsync(ProcessedIntegrationEvent.Create(eventId), ct);

    public Task<Account?> GetPersonalAccountAsync(Guid userId, CancellationToken ct = default) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public Task<Account?> GetGroupAccountAsync(Guid groupId, CancellationToken ct = default) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.GroupId == groupId, ct);

    public async Task AddAccountAsync(Account account, CancellationToken ct = default) =>
        await _db.Accounts.AddAsync(account, ct);

    public async Task AddLedgerEntryAsync(PointLedgerEntry entry, CancellationToken ct = default) =>
        await _db.LedgerEntries.AddAsync(entry, ct);

    public async Task<IReadOnlyList<PointLedgerEntry>> GetLedgerAsync(Guid accountId, int take, CancellationToken ct = default) =>
        await _db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
