using MediatR;
using TgTodo.Contracts;
using TgTodo.Identity.Application.Abstractions;
using TgTodo.Identity.Domain.Entities;

namespace TgTodo.Identity.Application.Users;

public record UpdateUserTimezoneCommand(Guid UserId, string Timezone) : IRequest<UserDto>;

public class UpdateUserTimezoneCommandHandler : IRequestHandler<UpdateUserTimezoneCommand, UserDto>
{
    private readonly IUserRepository _users;

    public UpdateUserTimezoneCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserDto> Handle(UpdateUserTimezoneCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        var tz = TimeZoneCalendar.NormalizeTimeZoneId(request.Timezone);
        user.SetTimezone(tz);
        await _users.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.TelegramId, user.DisplayName, user.Timezone);
    }
}
