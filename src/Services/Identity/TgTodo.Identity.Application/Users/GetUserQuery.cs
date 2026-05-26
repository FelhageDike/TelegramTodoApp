using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Identity.Application.Abstractions;

namespace TgTodo.Identity.Application.Users;

public record GetUserQuery(Guid UserId) : IRequest<UserDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _users;

    public GetUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User not found.");
        return new UserDto(user.Id, user.TelegramId, user.DisplayName, user.Timezone);
    }
}
