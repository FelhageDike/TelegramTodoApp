using Microsoft.EntityFrameworkCore;
using TgTodo.Identity.Application.Abstractions;
using TgTodo.Identity.Domain.Entities;
using TgTodo.Identity.Infrastructure.Persistence;

namespace TgTodo.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;

    public UserRepository(IdentityDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return Array.Empty<User>();

        return await _db.Users.Where(u => idList.Contains(u.Id)).ToListAsync(ct);
    }

    public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
