using MediatR;
using TgTodo.Identity.Application.Abstractions;
using TgTodo.Identity.Domain.Entities;

namespace TgTodo.Identity.Application.Users;

public record EnsureUserCommand(long TelegramId, string DisplayName, string Timezone) : IRequest<UserDto>;

public record UserDto(Guid Id, long TelegramId, string DisplayName, string Timezone);

public class EnsureUserCommandHandler : IRequestHandler<EnsureUserCommand, UserDto>
{
    private readonly IUserRepository _users;

    public EnsureUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserDto> Handle(EnsureUserCommand request, CancellationToken ct)
    {
        var existing = await _users.GetByTelegramIdAsync(request.TelegramId, ct);
        if (existing is not null)
        {
            existing.Update(request.DisplayName, request.Timezone);
            await _users.SaveChangesAsync(ct);
            return Map(existing);
        }

        var user = User.Create(request.TelegramId, request.DisplayName, request.Timezone);
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);
        return Map(user);
    }

    private static UserDto Map(User u) => new(u.Id, u.TelegramId, u.DisplayName, u.Timezone);
}
