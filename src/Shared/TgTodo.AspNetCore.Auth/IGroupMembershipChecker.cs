namespace TgTodo.AspNetCore.Auth;

public interface IGroupMembershipChecker
{
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
