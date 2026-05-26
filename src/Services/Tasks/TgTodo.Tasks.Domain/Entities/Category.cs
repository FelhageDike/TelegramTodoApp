using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Tasks.Domain.Entities;

public class Category : AuditableEntity
{
    public Guid? UserId { get; private set; }
    public Guid? GroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Emoji { get; private set; }

    private Category() { }

    public static Category CreatePersonal(Guid userId, string name, string? emoji) =>
        new() { UserId = userId, Name = name.Trim(), Emoji = emoji };

    public static Category CreateGroup(Guid groupId, string name, string? emoji) =>
        new() { GroupId = groupId, Name = name.Trim(), Emoji = emoji };
}
