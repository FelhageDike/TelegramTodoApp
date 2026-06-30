using TgTodo.Bff.Auth;
using TgTodo.Bff.Clients;

namespace TgTodo.Bff.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin/api");

        admin.MapGet("/auth/config", (IConfiguration config) =>
        {
            if (!AdminOidcConfig.SecretKeyAuthEnabled(config) && !AdminOidcConfig.IsEnabled(config))
                return Results.NotFound(new { error = "Admin dashboard is not configured." });

            return Results.Ok(new AdminAuthConfigDto(
                AdminOidcConfig.SecretKeyAuthEnabled(config),
                AdminOidcConfig.IsEnabled(config),
                AdminOidcConfig.Authority(config),
                AdminOidcConfig.ClientId(config),
                AdminOidcConfig.Scope(config),
                AdminOidcConfig.RequiredRole(config)));
        });

        admin.MapPost("/login", (LoginBody body, IConfiguration config) =>
        {
            var expectedKey = config["Admin:SecretKey"] ?? config["Admin__SecretKey"];
            if (string.IsNullOrEmpty(expectedKey))
                return Results.NotFound(new { error = "Admin dashboard is not configured." });

            if (string.IsNullOrWhiteSpace(body.Key) || body.Key != expectedKey)
                return Results.Json(new { error = "Invalid key" }, statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(new { ok = true });
        });

        admin.MapGet("/overview", async (
            IdentityApiClient identity,
            GamificationApiClient gamification,
            TasksApiClient tasks) =>
        {
            var usersTask = identity.GetAllUsersAsync();
            var balancesTask = gamification.GetAllPersonalBalancesAsync();
            var statsTask = tasks.GetAdminTaskStatsAsync();
            await Task.WhenAll(usersTask, balancesTask, statsTask);

            var users = await usersTask;
            var balanceByUser = (await balancesTask).ToDictionary(b => b.UserId, b => b.Balance);
            var statsByUser = (await statsTask).ToDictionary(s => s.UserId);

            var rows = users
                .Select(u =>
                {
                    var stats = statsByUser.GetValueOrDefault(u.Id);
                    return new AdminUserOverviewDto(
                        u.Id,
                        u.DisplayName,
                        u.TelegramId,
                        balanceByUser.GetValueOrDefault(u.Id),
                        stats?.ActiveTasks ?? 0,
                        stats?.CompletedTasks ?? 0);
                })
                .OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Results.Ok(rows);
        }).RequireAdminKey();

        admin.MapGet("/users/{userId:guid}/tasks", async (Guid userId, TasksApiClient tasks) =>
        {
            var userTasks = await tasks.GetAdminUserTasksAsync(userId);
            return Results.Ok(userTasks);
        }).RequireAdminKey();
    }
}

public record LoginBody(string Key);

public record AdminUserOverviewDto(
    Guid UserId,
    string DisplayName,
    long TelegramId,
    int Balance,
    int ActiveTasks,
    int CompletedTasks);
