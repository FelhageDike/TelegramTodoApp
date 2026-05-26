using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace TgTodo.MiniApp.Services;

public class UserTimeService
{
    private readonly IJSRuntime _js;
    private bool _initialized;

    public string? TimeZoneId { get; private set; }

    public event Action? OnChanged;

    public static IReadOnlyList<(string Id, string Label)> TimeZoneOptions { get; } =
    [
        ("", "Авто (устройство)"),
        ("Europe/Kaliningrad", "Калининград (UTC+2)"),
        ("Europe/Moscow", "Москва (UTC+3)"),
        ("Europe/Samara", "Самара (UTC+4)"),
        ("Asia/Yekaterinburg", "Екатеринбург (UTC+5)"),
        ("Asia/Omsk", "Омск (UTC+6)"),
        ("Asia/Krasnoyarsk", "Красноярск (UTC+7)"),
        ("Asia/Irkutsk", "Иркутск (UTC+8)"),
        ("Asia/Yakutsk", "Якутск (UTC+9)"),
        ("Asia/Vladivostok", "Владивосток (UTC+10)"),
        ("Asia/Magadan", "Магадан (UTC+11)"),
        ("Asia/Kamchatka", "Камчатка (UTC+12)"),
        ("Europe/Kyiv", "Киев (UTC+2/+3)"),
        ("Europe/Minsk", "Минск (UTC+3)"),
        ("Asia/Almaty", "Алматы (UTC+5/+6)"),
        ("Asia/Tbilisi", "Тбилиси (UTC+4)"),
        ("UTC", "UTC"),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public UserTimeService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        var settings = await LoadSettingsAsync();
        TimeZoneId = string.IsNullOrWhiteSpace(settings.TimeZoneId) ? null : settings.TimeZoneId;
    }

    public async Task SetTimeZoneAsync(string? timeZoneId)
    {
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId;
        var settings = await LoadSettingsAsync();
        settings.TimeZoneId = TimeZoneId;
        await SaveSettingsAsync(settings);
        OnChanged?.Invoke();
    }

    public async Task<DateOnly> GetTodayAsync()
    {
        await EnsureInitializedAsync();
        try
        {
            var iso = await _js.InvokeAsync<string>("tgTodoTime.getToday", TimeZoneId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(iso) && DateOnly.TryParse(iso, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }
        catch
        {
            // JS not ready or invalid response
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public async Task<DateTime> GetTodayDateTimeAsync()
    {
        var today = await GetTodayAsync();
        return today.ToDateTime(TimeOnly.MinValue);
    }

    public async Task<string> GetEffectiveTimeZoneLabelAsync()
    {
        await EnsureInitializedAsync();
        var tz = await _js.InvokeAsync<string>("tgTodoTime.getEffectiveTimeZone", TimeZoneId ?? string.Empty);
        if (string.IsNullOrEmpty(TimeZoneId))
            return $"Авто ({tz})";

        var match = TimeZoneOptions.FirstOrDefault(o => o.Id == TimeZoneId);
        return string.IsNullOrEmpty(match.Label) ? tz : match.Label;
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
            await InitializeAsync();
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
}
