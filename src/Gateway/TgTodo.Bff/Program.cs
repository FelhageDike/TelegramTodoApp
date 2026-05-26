using Serilog;
using TgTodo.Bff.Auth;
using TgTodo.Bff.Clients;
using TgTodo.Bff.Endpoints;
using TgTodo.Bff.Services;
using TgTodo.Contracts.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddSingleton<TelegramInitDataValidator>();
builder.Services.AddHttpClient<IdentityApiClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Services:Identity"]!);
});
builder.Services.AddHttpClient<GroupsApiClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Services:Groups"]!);
});
builder.Services.AddHttpClient<TasksApiClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Services:Tasks"]!);
});
builder.Services.AddHttpClient<GamificationApiClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Services:Gamification"]!);
});
builder.Services.AddSingleton<BotInlineDraftStore>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        if (path.EndsWith("index.html", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.Ordinal) ||
            path.EndsWith("app.css", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
    }
});
app.UseMiddleware<TelegramAuthMiddleware>();
app.MapPost("/bff/internal/bot/drafts", (InlineTaskDraftDto draft, BotInlineDraftStore store) =>
{
    store.Upsert(draft);
    return Results.Ok();
});
app.MapGet("/bff/internal/bot/drafts/{id}", (string id, BotInlineDraftStore store) =>
{
    var d = store.Get(id);
    return d is null ? Results.NotFound() : Results.Json(d);
});
app.MapDelete("/bff/internal/bot/drafts/{id}", (string id, BotInlineDraftStore store) =>
{
    store.Delete(id);
    return Results.NoContent();
});
app.MapPost("/bff/internal/bot/drafts/prune", (BotInlineDraftStore store) =>
{
    store.CleanupExpired();
    return Results.Ok();
});
app.MapBffEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "bff" }));
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.Contains('.', StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
    context.Response.ContentType = "text/html";
    context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();
