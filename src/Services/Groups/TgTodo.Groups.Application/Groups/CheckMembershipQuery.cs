using MediatR;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Application.Groups;

public record CheckMembershipQuery(Guid UserId, Guid GroupId) : IRequest<bool>;

public class CheckMembershipQueryHandler : IRequestHandler<CheckMembershipQuery, bool>
{
    private readonly IGroupRepository _groups;

    public CheckMembershipQueryHandler(IGroupRepository groups) => _groups = groups;

    public Task<bool> Handle(CheckMembershipQuery request, CancellationToken ct) =>
        _groups.IsMemberAsync(request.GroupId, request.UserId, ct);
}
