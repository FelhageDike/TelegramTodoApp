using TgTodo.AspNetCore.Auth;
using TgTodo.Groups.Application.Abstractions;

namespace TgTodo.Groups.Api.Auth;

public sealed class GroupRepositoryMembershipChecker(IGroupRepository groups) : IGroupMembershipChecker
{
    public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        groups.IsMemberAsync(groupId, userId, cancellationToken);
}
