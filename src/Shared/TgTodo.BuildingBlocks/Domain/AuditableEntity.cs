namespace TgTodo.BuildingBlocks.Domain;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}
