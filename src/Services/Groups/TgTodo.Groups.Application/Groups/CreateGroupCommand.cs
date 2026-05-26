using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Groups.Application.Abstractions;
using TgTodo.Groups.Domain.Entities;

namespace TgTodo.Groups.Application.Groups;

public record CreateGroupCommand(Guid UserId, string Name) : IRequest<GroupDto>;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groups;

    public CreateGroupCommandHandler(IGroupRepository groups) => _groups = groups;

    public async Task<GroupDto> Handle(CreateGroupCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Название группы не может быть пустым.");

        var code = GenerateInviteCode();
        var group = FamilyGroup.Create(request.Name.Trim(), request.UserId, code);
        group.Members.Add(GroupMember.Create(group.Id, request.UserId, GroupRole.Owner));
        await _groups.AddAsync(group, ct);
        await _groups.SaveChangesAsync(ct);
        return new GroupDto(group.Id, group.Name, group.InviteCode, GroupRole.Owner);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
