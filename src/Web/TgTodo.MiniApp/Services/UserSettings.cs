namespace TgTodo.MiniApp.Services;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public class UserSettings
{
    public string PrimaryColor { get; set; } = "#5B7FFF";
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public bool UseTelegramColors { get; set; } = true;
    public string? TimeZoneId { get; set; }
}
