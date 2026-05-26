using TgTodo.BuildingBlocks.Domain;
using TgTodo.Contracts.Enums;

namespace TgTodo.Gamification.Domain.Entities;

public class Account : AuditableEntity
{
    public AccountType Type { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? GroupId { get; private set; }
    public int Balance { get; private set; }

    private Account() { }

    public static Account CreatePersonal(Guid userId) =>
        new() { Type = AccountType.Personal, UserId = userId };

    public static Account CreateGroup(Guid groupId) =>
        new() { Type = AccountType.Group, GroupId = groupId };

    public void ApplyDelta(int delta)
    {
        var newBalance = Balance + delta;
        if (newBalance < 0)
            throw new InvalidOperationException("Insufficient points.");
        Balance = newBalance;
        MarkUpdated();
    }
}
