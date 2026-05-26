using Microsoft.JSInterop;

namespace TgTodo.MiniApp.Services;

public class TelegramThemeParams
{
    public string ColorScheme { get; set; } = "light";
    public string? BgColor { get; set; }
    public string? SecondaryBgColor { get; set; }
    public string? TextColor { get; set; }
    public string? HintColor { get; set; }
    public string? LinkColor { get; set; }
    public string? ButtonColor { get; set; }
    public string? ButtonTextColor { get; set; }
}

public class TelegramUserInfo
{
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Username { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}".Trim();

    public string Initials
    {
        get
        {
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}

public class TelegramInterop
{
    private readonly IJSRuntime _js;

    public TelegramInterop(IJSRuntime js) => _js = js;

    public async Task<string?> GetInitDataAsync() =>
        await _js.InvokeAsync<string?>("tgTodoTelegram.getInitData");

    public async Task<string> GetColorSchemeAsync() =>
        await _js.InvokeAsync<string>("tgTodoTelegram.getTheme") ?? "light";

    public async Task<TelegramThemeParams?> GetThemeParamsAsync()
    {
        try
        {
            return await _js.InvokeAsync<TelegramThemeParams?>("tgTodoTelegram.getThemeParams");
        }
        catch
        {
            return null;
        }
    }

    public async Task<TelegramUserInfo?> GetUserAsync()
    {
        try
        {
            return await _js.InvokeAsync<TelegramUserInfo?>("tgTodoTelegram.getUser");
        }
        catch
        {
            return null;
        }
    }
}
