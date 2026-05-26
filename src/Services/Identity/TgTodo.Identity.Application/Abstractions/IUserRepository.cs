using TgTodo.Identity.Domain.Entities;

namespace TgTodo.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
