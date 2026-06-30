namespace TgTodo.BuildingBlocks.Abstractions;

public interface IGroupsMembershipClient
{
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
