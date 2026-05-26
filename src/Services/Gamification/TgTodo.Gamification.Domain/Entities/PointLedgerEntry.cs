using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Gamification.Domain.Entities;

public class PointLedgerEntry : Entity
{
    public Guid AccountId { get; private set; }
    public int Delta { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private PointLedgerEntry() { }

    public static PointLedgerEntry Create(Guid accountId, int delta, string reason, Guid? referenceId) =>
        new() { AccountId = accountId, Delta = delta, Reason = reason, ReferenceId = referenceId };
}
