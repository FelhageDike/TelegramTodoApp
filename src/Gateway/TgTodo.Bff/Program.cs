using Serilog;
using TgTodo.Bff.Auth;
using TgTodo.Bff.Clients;
using TgTodo.Bff.Endpoints;

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
