using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Groups.Application.Abstractions;
using TgTodo.Groups.Domain.Entities;

namespace TgTodo.Groups.Application.Groups;

public record JoinGroupCommand(Guid UserId, string InviteCode) : IRequest<GroupDto>;

public class JoinGroupCommandHandler : IRequestHandler<JoinGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groups;

    public JoinGroupCommandHandler(IGroupRepository groups) => _groups = groups;

    public async Task<GroupDto> Handle(JoinGroupCommand request, CancellationToken ct)
    {
        var code = InviteCodeNormalizer.Normalize(request.InviteCode);
        if (code.Length != 6)
            throw new NotFoundException("Invalid invite code.");

        var group = await _groups.GetByInviteCodeAsync(code, ct)
            ?? throw new NotFoundException("Group not found.");

        if (await _groups.IsMemberAsync(group.Id, request.UserId, ct))
            return new GroupDto(group.Id, group.Name, group.InviteCode, GroupRole.Member);

        await _groups.AddMemberAsync(
            GroupMember.Create(group.Id, request.UserId, GroupRole.Member), ct);
        await _groups.SaveChangesAsync(ct);
        return new GroupDto(group.Id, group.Name, group.InviteCode, GroupRole.Member);
    }
}
