using MediatR;
using TgTodo.Identity.Application.Abstractions;

namespace TgTodo.Identity.Application.Users;

public record GetUsersByIdsQuery(IReadOnlyList<Guid> UserIds) : IRequest<IReadOnlyList<UserDto>>;

public class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _users;

    public GetUsersByIdsQueryHandler(IUserRepository users) => _users = users;

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersByIdsQuery request, CancellationToken ct)
    {
        var users = await _users.GetByIdsAsync(request.UserIds, ct);
        return users.Select(u => new UserDto(u.Id, u.TelegramId, u.DisplayName, u.Timezone)).ToList();
    }
}
