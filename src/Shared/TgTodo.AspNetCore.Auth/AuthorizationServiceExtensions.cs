using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TgTodo.AspNetCore.Auth;

public static class AuthorizationServiceExtensions
{
    public static Task<AuthorizationResult> AuthorizeGroupMemberAsync(
        this IAuthorizationService authorization,
        ClaimsPrincipal user,
        Guid? groupId,
        CancellationToken cancellationToken = default)
    {
        if (!groupId.HasValue)
            return Task.FromResult(AuthorizationResult.Success());

        return authorization.AuthorizeAsync(
            user,
            groupId.Value,
            TgTodoAuthDefaults.GroupMemberPolicy);
    }
}
