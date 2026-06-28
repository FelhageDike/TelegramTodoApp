namespace TgTodo.AspNetCore.Auth;

public static class TgTodoAuthDefaults
{
    public const string SchemeName = "TgTodoUser";
    public const string UserIdHeaderName = "X-User-Id";
    public const string UserIdClaimType = "tgtodo:user_id";
}
