using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Application.Groups;

public record LeaveGroupCommand(Guid UserId, Guid GroupId) : IRequest;

public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand>
{
    private readonly IGroupRepository _groups;

    public LeaveGroupCommandHandler(IGroupRepository groups) => _groups = groups;

    public async Task Handle(LeaveGroupCommand request, CancellationToken ct)
    {
        var group = await _groups.GetByIdAsync(request.GroupId, ct)
            ?? throw new NotFoundException("Group not found.");

        var member = await _groups.GetMemberAsync(request.GroupId, request.UserId, ct)
            ?? throw new NotFoundException("You are not a member of this group.");

        if (member.Role == GroupRole.Owner)
            throw new ForbiddenException("Владелец не может выйти. Удалите группу, если она больше не нужна.");

        await _groups.RemoveMemberAsync(request.GroupId, request.UserId, ct);
        await _groups.SaveChangesAsync(ct);
    }
}
