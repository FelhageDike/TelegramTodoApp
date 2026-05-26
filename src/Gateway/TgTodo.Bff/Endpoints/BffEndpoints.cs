using TgTodo.Bff.Auth;
using TgTodo.Bff.Clients;
using TgTodo.Contracts;

namespace TgTodo.Bff.Endpoints;

public static class BffEndpoints
{
    public static void MapBffEndpoints(this WebApplication app)
    {
        var bff = app.MapGroup("/bff");

        bff.MapGet("/me", (HttpContext ctx) =>
        {
            var user = ctx.GetUserContext();
            return Results.Ok(new UserProfileDto(user.UserId, user.DisplayName, user.Timezone));
        });

        bff.MapPatch("/profile/timezone", async (HttpContext ctx, UpdateTimezoneBody body, IdentityApiClient identity) =>
        {
            if (!TimeZoneCalendar.IsValidTimeZoneId(body.Timezone))
                return Results.BadRequest(new { error = "Некорректный часовой пояс (ожидается IANA, например Europe/Samara)." });

            var user = ctx.GetUserContext();
            var updated = await identity.UpdateTimezoneAsync(user.UserId, body.Timezone.Trim());
            user.Timezone = updated.Timezone;
            return Results.Ok(new UserProfileDto(updated.Id, updated.DisplayName, updated.Timezone));
        });

        bff.MapGet("/home", async (
            HttpContext ctx,
            Guid? groupId,
            DateOnly? date,
            GroupsApiClient groups,
            TasksApiClient tasks,
            GamificationApiClient gamification,
            IdentityApiClient identity) =>
        {
            var user = ctx.GetUserContext();
            var day = date ?? TimeZoneCalendar.Today(user.Timezone);
            var groupList = await groups.GetGroupsAsync(user.UserId);
            var balance = await gamification.GetBalanceAsync(user.UserId, groupId);
            var taskList = await tasks.GetTasksAsync(user.UserId, groupId, day);
            var members = groupId.HasValue
                ? await ResolveGroupMembersAsync(user.UserId, groupId.Value, groups, identity)
                : null;
            return Results.Ok(new HomeDto(user.UserId, balance, taskList, groupList, members));
        });

        bff.MapGet("/home/month", async (
            HttpContext ctx,
            Guid? groupId,
            int year,
            int month,
            TasksApiClient tasks) =>
        {
            var user = ctx.GetUserContext();
            if (year < 2000 || year > 2100)
                year = DateTime.UtcNow.Year;
            if (month is < 1 or > 12)
                month = DateTime.UtcNow.Month;
            var monthTasks = await tasks.GetMonthTasksAsync(user.UserId, groupId, year, month);
            return Results.Ok(monthTasks);
        });

        bff.MapGet("/groups/{groupId:guid}/members", async (
            HttpContext ctx,
            Guid groupId,
            GroupsApiClient groups,
            IdentityApiClient identity) =>
        {
            var user = ctx.GetUserContext();
            var members = await ResolveGroupMembersAsync(user.UserId, groupId, groups, identity);
            return Results.Ok(members);
        });

        bff.MapGet("/groups", async (HttpContext ctx, GroupsApiClient groups) =>
        {
            var user = ctx.GetUserContext();
            return Results.Ok(await groups.GetGroupsAsync(user.UserId));
        });

        bff.MapPost("/groups", async (HttpContext ctx, CreateGroupBody body, GroupsApiClient groups) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { error = "Название группы не может быть пустым." });
            return await BffResult.From(async () =>
            {
                var user = ctx.GetUserContext();
                return await groups.CreateGroupAsync(user.UserId, body.Name.Trim());
            });
        });

        bff.MapPost("/groups/join", async (HttpContext ctx, JoinGroupBody body, GroupsApiClient groups) =>
        {
            return await BffResult.From(async () =>
            {
                var user = ctx.GetUserContext();
                return await groups.JoinGroupAsync(user.UserId, body.InviteCode);
            });
        });

        bff.MapPost("/groups/{groupId:guid}/leave", async (
            HttpContext ctx,
            Guid groupId,
            GroupsApiClient groups) =>
        {
            return await BffResult.From(async () =>
            {
                var user = ctx.GetUserContext();
                await groups.LeaveGroupAsync(user.UserId, groupId);
            });
        });

        bff.MapDelete("/groups/{groupId:guid}", async (
            HttpContext ctx,
            Guid groupId,
            GroupsApiClient groups) =>
        {
            return await BffResult.From(async () =>
            {
                var user = ctx.GetUserContext();
                await groups.DeleteGroupAsync(user.UserId, groupId);
            });
        });

        bff.MapPost("/tasks", async (HttpContext ctx, CreateTaskBody body, TasksApiClient tasks) =>
        {
            if (string.IsNullOrWhiteSpace(body.Title))
                return Results.BadRequest(new { error = "Название задачи не может быть пустым." });
            return await BffResult.From(async () =>
            {
                var user = ctx.GetUserContext();
                return await tasks.CreateTaskAsync(user.UserId, body with { Title = body.Title.Trim() });
            });
        });

        bff.MapPost("/tasks/{taskId:guid}/complete", async (
            HttpContext ctx,
            Guid taskId,
            DateOnly? date,
            TasksApiClient tasks) =>
        {
            var user = ctx.GetUserContext();
            var task = await tasks.CompleteTaskAsync(user.UserId, taskId, date);
            return Results.Ok(task);
        });

        bff.MapGet("/categories", async (HttpContext ctx, Guid? groupId, TasksApiClient tasks) =>
        {
            var user = ctx.GetUserContext();
            return Results.Ok(await tasks.GetCategoriesAsync(user.UserId, groupId));
        });

        bff.MapGet("/balance", async (HttpContext ctx, Guid? groupId, GamificationApiClient gamification) =>
        {
            var user = ctx.GetUserContext();
            return Results.Ok(await gamification.GetBalanceAsync(user.UserId, groupId));
        });

        bff.MapGet("/ledger", async (
            HttpContext ctx,
            Guid? groupId,
            GamificationApiClient gamification,
            int take = 50) =>
        {
            var user = ctx.GetUserContext();
            return Results.Ok(await gamification.GetLedgerAsync(user.UserId, groupId, take > 0 ? take : 50));
        });
    }

    private static async Task<IReadOnlyList<GroupMemberViewDto>> ResolveGroupMembersAsync(
        Guid userId,
        Guid groupId,
        GroupsApiClient groups,
        IdentityApiClient identity)
    {
        var members = await groups.GetMembersAsync(userId, groupId);
        if (members.Count == 0)
            return Array.Empty<GroupMemberViewDto>();

        var userIds = members.Select(m => m.UserId).ToList();
        var users = await identity.GetUsersByIdsAsync(userIds);
        var names = users.ToDictionary(u => u.Id, u => u.DisplayName);

        return members.Select(m => new GroupMemberViewDto(
            m.UserId,
            names.GetValueOrDefault(m.UserId, "Участник"),
            m.Role,
            m.JoinedAt)).ToList();
    }

    private record UserProfileDto(Guid UserId, string DisplayName, string Timezone);
    private record UpdateTimezoneBody(string Timezone);
    private record CreateGroupBody(string Name);
    private record JoinGroupBody(string InviteCode);
    private record CreateTaskBody(
        TgTodo.Contracts.Enums.TaskScope Scope,
        string Title,
        int PointsReward,
        TgTodo.Contracts.Enums.RecurrenceType Recurrence,
        int? Weekday,
        int? DayOfMonth,
        int? IntervalDays,
        TgTodo.Contracts.Enums.PersonalTaskVisibility PersonalVisibility,
        TgTodo.Contracts.Enums.CompletionMode CompletionMode,
        Guid? GroupId,
        Guid? AssignedToUserId,
        Guid? CategoryId,
        Guid? VisibilityGroupId,
        DateOnly? StartDate);
}
