using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Gamification.Domain.Entities;

public class ProcessedIntegrationEvent : Entity
{
    public Guid EventId { get; private set; }
    public DateTime ProcessedAt { get; private set; } = DateTime.UtcNow;

    private ProcessedIntegrationEvent() { }

    public static ProcessedIntegrationEvent Create(Guid eventId) =>
        new() { EventId = eventId };
}
