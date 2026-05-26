using TgTodo.BuildingBlocks.Domain;
using TgTodo.Contracts.Enums;

namespace TgTodo.Groups.Domain.Entities;

public class GroupMember : Entity
{
    public Guid GroupId { get; private set; }
    public FamilyGroup Group { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public GroupRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    private GroupMember() { }

    public static GroupMember Create(Guid groupId, Guid userId, GroupRole role) =>
        new() { GroupId = groupId, UserId = userId, Role = role };
}
