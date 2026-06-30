using MediatR;
using TgTodo.Identity.Application.Abstractions;

namespace TgTodo.Identity.Application.Users;

public record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _users;

    public GetAllUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<IReadOnlyList<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return users
            .Select(u => new UserDto(u.Id, u.TelegramId, u.DisplayName, u.Timezone))
            .ToList();
    }
}
