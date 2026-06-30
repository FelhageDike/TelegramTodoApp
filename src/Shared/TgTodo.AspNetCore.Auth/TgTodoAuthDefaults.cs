namespace TgTodo.AspNetCore.Auth;

public static class TgTodoAuthDefaults
{
    public const string UserSchemeName = "TgTodoUser";
    public const string InternalSchemeName = "TgTodoInternal";

    public const string InternalPolicy = "TgTodoInternal";
    public const string GroupMemberPolicy = "TgTodoGroupMember";
    public const string GroupMemberOptionalPolicy = "TgTodoGroupMemberOptional";

    public const string UserIdHeaderName = "X-User-Id";
    public const string UserIdSignatureHeaderName = "X-User-Id-Signature";
    public const string ServiceKeyHeaderName = "X-TgTodo-Service-Key";

    public const string UserIdClaimType = "tgtodo:user_id";

    /// <summary>Default scheme for user-facing API (signed X-User-Id).</summary>
    public const string SchemeName = UserSchemeName;
}
