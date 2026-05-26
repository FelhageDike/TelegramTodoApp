namespace TgTodo.Tasks.Application.Abstractions;

public interface IGroupsClient
{
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
}
