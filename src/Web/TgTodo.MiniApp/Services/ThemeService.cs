using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MudBlazor;

namespace TgTodo.MiniApp.Services;

public class ThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly TelegramInterop _telegram;
    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private bool _initialized;

    public string PrimaryColor { get; private set; } = "#5B7FFF";
    public AppThemeMode ThemeMode { get; private set; } = AppThemeMode.System;
    public bool UseTelegramColors { get; private set; } = true;
    public bool IsDarkMode { get; private set; }
    public MudTheme Theme { get; private set; } = CreateTheme("#5B7FFF", false);
    public event Action? OnChanged;

    public static IReadOnlyList<(string Name, string Hex)> Presets { get; } =
    [
        ("Синий", "#5B7FFF"),
        ("Фиолетовый", "#7C5CFC"),
        ("Зелёный", "#22C55E"),
        ("Оранжевый", "#F97316"),
        ("Розовый", "#EC4899"),
        ("Бирюзовый", "#14B8A6"),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ThemeService(IJSRuntime js, TelegramInterop telegram)
    {
        _js = js;
        _telegram = telegram;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        var settings = await LoadSettingsAsync();
        ThemeMode = settings.ThemeMode;
        UseTelegramColors = settings.UseTelegramColors;
        PrimaryColor = settings.PrimaryColor;

        _dotNetRef = DotNetObjectReference.Create(this);
        try
        {
            await _js.InvokeVoidAsync("tgTodoTelegram.onThemeChanged", _dotNetRef);
        }
        catch
        {
            // dev browser
        }

        await ApplyAsync();
    }

    public async Task SetPrimaryColorAsync(string hex)
    {
        PrimaryColor = hex;
        UseTelegramColors = false;
        var settings = await LoadSettingsAsync();
        settings.PrimaryColor = hex;
        settings.UseTelegramColors = false;
        await SaveSettingsAsync(settings);
        await ApplyAsync(primaryOverride: hex);
    }

    public async Task SetThemeModeAsync(AppThemeMode mode)
    {
        ThemeMode = mode;
        var settings = await LoadSettingsAsync();
        settings.ThemeMode = mode;
        await SaveSettingsAsync(settings);
        await ApplyAsync();
    }

    public async Task SetUseTelegramColorsAsync(bool value)
    {
        UseTelegramColors = value;
        var settings = await LoadSettingsAsync();
        settings.UseTelegramColors = value;
        await SaveSettingsAsync(settings);
        await ApplyAsync();
    }

    [JSInvokable]
    public async Task OnTelegramThemeChanged()
    {
        await ApplyAsync();
    }

    private async Task ApplyAsync(string? primaryOverride = null)
    {
        var isDark = await ResolveIsDarkAsync();
        var primary = primaryOverride ?? await ResolvePrimaryColorAsync(isDark);
        var palette = GetPalette(isDark, primary);

        PrimaryColor = primary;
        IsDarkMode = isDark;
        Theme = CreateTheme(primary, isDark);

        try
        {
            await _js.InvokeVoidAsync("tgTodoTheme.applyTheme", new
            {
                primaryColor = palette.Primary,
                isDark,
                background = palette.Background,
                surface = palette.Surface,
                text = palette.Text,
                textMuted = palette.TextMuted,
                themeMode = ThemeMode.ToString().ToLowerInvariant()
            });
        }
        catch
        {
            // ignore JS errors in dev
        }

        OnChanged?.Invoke();
    }

    private static (string Primary, string Background, string Surface, string Text, string TextMuted) GetPalette(
        bool isDark, string primary) =>
        isDark
            ? (primary, "#0f172a", "#1e293b", "#f1f5f9", "#94a3b8")
            : (primary, "#f3f4f8", "#ffffff", "#1e293b", "#64748b");

    private async Task<bool> ResolveIsDarkAsync()
    {
        if (ThemeMode == AppThemeMode.Dark) return true;
        if (ThemeMode == AppThemeMode.Light) return false;

        try
        {
            return await _js.InvokeAsync<bool>("tgTodoTheme.resolveIsDark");
        }
        catch
        {
            var scheme = await _telegram.GetColorSchemeAsync();
            return scheme == "dark";
        }
    }

    private async Task<string> ResolvePrimaryColorAsync(bool isDark)
    {
        var settings = await LoadSettingsAsync();
        if (UseTelegramColors && ThemeMode == AppThemeMode.System)
        {
            var tg = await _telegram.GetThemeParamsAsync();
            if (!string.IsNullOrWhiteSpace(tg?.ButtonColor))
                return tg.ButtonColor!;
        }

        return settings.PrimaryColor;
    }

    private async Task<UserSettings> LoadSettingsAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string>("tgTodoSettings.loadJson");
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    private async Task SaveSettingsAsync(UserSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await _js.InvokeVoidAsync("tgTodoSettings.saveJson", json);
    }

    private static MudTheme CreateTheme(string primary, bool isDark)
    {
        if (isDark)
        {
            return new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Primary = primary,
                    Secondary = "#94A3B8",
                    Background = "#0f172a",
                    Surface = "#1e293b",
                    AppbarBackground = "#1e293b",
                    DrawerBackground = "#1e293b",
                    TextPrimary = "#f1f5f9",
                    TextSecondary = "#94a3b8",
                }
            };
        }

        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = primary,
                Secondary = "#94A3B8",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#1E293B",
                Background = "#F3F4F8",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                TextPrimary = "#1E293B",
                TextSecondary = "#64748B",
                ActionDefault = "#64748B",
                Divider = "#E2E8F0",
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
    }
}
