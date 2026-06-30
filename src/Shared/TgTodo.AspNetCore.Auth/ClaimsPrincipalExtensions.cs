using System.Security.Claims;

namespace TgTodo.AspNetCore.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(TgTodoAuthDefaults.UserIdClaimType);
        if (value is null || !Guid.TryParse(value, out var userId))
            throw new InvalidOperationException("Authenticated user id claim is missing or invalid.");

        return userId;
    }
}
