using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Application.Groups;

public record GetMembersQuery(Guid UserId, Guid GroupId) : IRequest<IReadOnlyList<GroupMemberDto>>;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, IReadOnlyList<GroupMemberDto>>
{
    private readonly IGroupRepository _groups;

    public GetMembersQueryHandler(IGroupRepository groups) => _groups = groups;

    public async Task<IReadOnlyList<GroupMemberDto>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        if (!await _groups.IsMemberAsync(request.GroupId, request.UserId, ct))
            throw new ForbiddenException("Not a group member.");

        var group = await _groups.GetByIdAsync(request.GroupId, ct)
            ?? throw new NotFoundException("Group not found.");

        return group.Members
            .Select(m => new GroupMemberDto(m.UserId, m.Role, m.JoinedAt))
            .ToList();
    }
}
