using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Application.Groups;

public record DeleteGroupCommand(Guid UserId, Guid GroupId) : IRequest;

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand>
{
    private readonly IGroupRepository _groups;

    public DeleteGroupCommandHandler(IGroupRepository groups) => _groups = groups;

    public async Task Handle(DeleteGroupCommand request, CancellationToken ct)
    {
        var group = await _groups.GetByIdAsync(request.GroupId, ct)
            ?? throw new NotFoundException("Group not found.");

        var member = await _groups.GetMemberAsync(request.GroupId, request.UserId, ct)
            ?? throw new ForbiddenException("Not a group member.");

        if (member.Role != GroupRole.Owner)
            throw new ForbiddenException("Only the group owner can delete the group.");

        await _groups.DeleteGroupAsync(request.GroupId, ct);
        await _groups.SaveChangesAsync(ct);
    }
}
