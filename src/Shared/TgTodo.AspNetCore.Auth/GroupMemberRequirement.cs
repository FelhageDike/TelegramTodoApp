using Microsoft.AspNetCore.Authorization;

namespace TgTodo.AspNetCore.Auth;

public sealed class GroupMemberRequirement : IAuthorizationRequirement
{
    public bool AllowMissingGroupId { get; init; }

    public string RouteParameterName { get; init; } = "groupId";

    public string QueryParameterName { get; init; } = "groupId";
}
