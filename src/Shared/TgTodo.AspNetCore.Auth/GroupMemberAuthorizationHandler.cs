using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace TgTodo.AspNetCore.Auth;

public sealed class GroupMemberAuthorizationHandler : AuthorizationHandler<GroupMemberRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGroupMembershipChecker _membershipChecker;

    public GroupMemberAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IGroupMembershipChecker membershipChecker)
    {
        _httpContextAccessor = httpContextAccessor;
        _membershipChecker = membershipChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupMemberRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        Guid userId;
        try
        {
            userId = context.User.GetUserId();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        var groupId = ResolveGroupId(context, requirement);
        if (groupId is null)
        {
            if (requirement.AllowMissingGroupId)
                context.Succeed(requirement);

            return;
        }

        if (await _membershipChecker.IsMemberAsync(groupId.Value, userId))
            context.Succeed(requirement);
    }

    private Guid? ResolveGroupId(AuthorizationHandlerContext context, GroupMemberRequirement requirement)
    {
        if (context.Resource is Guid groupId)
            return groupId;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        if (httpContext.Request.RouteValues.TryGetValue(requirement.RouteParameterName, out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var routeGroupId))
        {
            return routeGroupId;
        }

        if (httpContext.Request.Query.TryGetValue(requirement.QueryParameterName, out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out var queryGroupId))
        {
            return queryGroupId;
        }

        return null;
    }
}
