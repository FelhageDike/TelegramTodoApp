using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Groups.Domain.Entities;

public class FamilyGroup : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = "family";
    public string InviteCode { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public string SettingsJson { get; private set; } = "{}";
    public ICollection<GroupMember> Members { get; private set; } = new List<GroupMember>();

    private FamilyGroup() { }

    public static FamilyGroup Create(string name, Guid createdByUserId, string inviteCode)
    {
        return new FamilyGroup
        {
            Name = name,
            CreatedByUserId = createdByUserId,
            InviteCode = inviteCode
        };
    }
}
