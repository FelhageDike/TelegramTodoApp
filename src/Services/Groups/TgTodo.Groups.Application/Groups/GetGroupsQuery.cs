using MediatR;
using TgTodo.Contracts.Enums;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Application.Groups;

public record GetGroupsQuery(Guid UserId) : IRequest<IReadOnlyList<GroupDto>>;

public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, IReadOnlyList<GroupDto>>
{
    private readonly IGroupRepository _groups;

    public GetGroupsQueryHandler(IGroupRepository groups) => _groups = groups;

    public async Task<IReadOnlyList<GroupDto>> Handle(GetGroupsQuery request, CancellationToken ct)
    {
        var groups = await _groups.GetByUserIdAsync(request.UserId, ct);
        return groups.Select(g =>
        {
            var role = g.Members.First(m => m.UserId == request.UserId).Role;
            return new GroupDto(g.Id, g.Name, g.InviteCode, role);
        }).ToList();
    }
}
