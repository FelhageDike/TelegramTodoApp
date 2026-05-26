using System.Text;
using TgTodo.Bff.Clients;

namespace TgTodo.Bff.Auth;

public class TelegramAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TelegramAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        TelegramInitDataValidator validator,
        IdentityApiClient identityClient,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        var path = context.Request.Path;

        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Черновики inline: только ключ бота (без initData и без Identity на этом шаге не нужен).
        var pathValue = path.Value ?? "";
        if (pathValue.StartsWith("/bff/internal/bot/drafts", StringComparison.Ordinal))
        {
            var internalKey = configuration["Bot:InternalKey"] ?? configuration["Bot__InternalKey"];
            if (string.IsNullOrEmpty(internalKey) ||
                !context.Request.Headers.TryGetValue("X-TgTodo-Bot-Key", out var draftKey) ||
                draftKey != internalKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }

            await _next(context);
            return;
        }

        if (!path.StartsWithSegments("/bff"))
        {
            await _next(context);
            return;
        }

        TelegramUser? telegramUser = null;

        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            if (value.StartsWith("tma ", StringComparison.OrdinalIgnoreCase))
            {
                var initData = value["tma ".Length..];
                validator.TryValidate(initData, out telegramUser);
            }
        }

        if (telegramUser is null &&
            context.Request.Headers.TryGetValue("X-TgTodo-Bot-Key", out var botKey) &&
            context.Request.Headers.TryGetValue("X-Telegram-User-Id", out var botTgId) &&
            long.TryParse(botTgId, out var botTelegramId))
        {
            var expectedKey = configuration["Bot:InternalKey"] ?? configuration["Bot__InternalKey"];
            if (!string.IsNullOrEmpty(expectedKey) && botKey == expectedKey)
            {
                context.Request.Headers.TryGetValue("X-Telegram-Display-Name", out var tgDisplayHeader);
                var name = tgDisplayHeader.Count > 0
                    ? DecodeBotDisplayName(tgDisplayHeader.ToString())
                    : "User";
                telegramUser = new TelegramUser(botTelegramId, name, null, null);
            }
        }

        if (telegramUser is null && env.IsDevelopment() &&
            context.Request.Headers.TryGetValue("X-Dev-Telegram-Id", out var devId) &&
            long.TryParse(devId, out var telegramId))
        {
            telegramUser = new TelegramUser(telegramId, "Dev", null, "dev");
        }

        if (telegramUser is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        var displayName = string.Join(' ', new[] { telegramUser.FirstName, telegramUser.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var user = await identityClient.EnsureUserAsync(
            telegramUser.Id,
            string.IsNullOrWhiteSpace(displayName) ? telegramUser.Username ?? "User" : displayName,
            "UTC");

        context.Items["UserContext"] = new UserContext
        {
            UserId = user.Id,
            TelegramId = user.TelegramId,
            DisplayName = user.DisplayName
        };

        await _next(context);
    }

    private static string DecodeBotDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "User";

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static bool IsPublicPath(PathString path) =>
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/_framework") ||
        path.StartsWithSegments("/_content") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/TgTodo.MiniApp") ||
        path.StartsWithSegments("/favicon") ||
        path.Value?.EndsWith(".css", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".js", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) == true;
}

public static class HttpContextExtensions
{
    public static UserContext GetUserContext(this HttpContext context) =>
        (UserContext)context.Items["UserContext"]!;
}
