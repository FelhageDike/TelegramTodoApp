namespace TgTodo.Contracts;

public static class TimeZoneCalendar
{
    public static DateOnly Today(string? timeZoneId)
    {
        var tz = ResolveTimeZone(timeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(local);
    }

    public static bool IsValidTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public static string NormalizeTimeZoneId(string? timeZoneId) =>
        IsValidTimeZoneId(timeZoneId) ? timeZoneId!.Trim() : "UTC";

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (IsValidTimeZoneId(id))
            return TimeZoneInfo.FindSystemTimeZoneById(id!.Trim());

        return TimeZoneInfo.Utc;
    }
}
