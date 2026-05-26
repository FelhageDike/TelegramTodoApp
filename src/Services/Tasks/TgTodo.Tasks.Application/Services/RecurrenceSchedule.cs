using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Services;

public static class RecurrenceSchedule
{
    public static bool IsDueOnDate(TodoTask task, DateOnly date)
    {
        if (date < task.RecurrenceStartDate)
            return false;

        return task.Recurrence switch
        {
            RecurrenceType.None => date == task.RecurrenceStartDate,
            RecurrenceType.Daily => true,
            RecurrenceType.Weekly => task.Weekday.HasValue &&
                                      (int)date.DayOfWeek == NormalizeWeekday(task.Weekday.Value),
            RecurrenceType.Monthly => task.DayOfMonth.HasValue && MatchesDayOfMonth(date, task.DayOfMonth.Value),
            RecurrenceType.EveryNDays => task.IntervalDays is > 0 &&
                DaysSinceAnchor(task, date) >= 0 &&
                DaysSinceAnchor(task, date) % task.IntervalDays.Value == 0,
            _ => false
        };
    }

    public static string GetPeriodKey(TodoTask task, DateOnly date)
    {
        return task.Recurrence switch
        {
            RecurrenceType.None => "once",
            RecurrenceType.Daily => date.ToString("yyyy-MM-dd"),
            RecurrenceType.Weekly => GetWeeklyPeriodKey(task, date),
            RecurrenceType.Monthly => $"{date.Year}-{date.Month:D2}",
            RecurrenceType.EveryNDays => date.ToString("yyyy-MM-dd"),
            _ => "once"
        };
    }

    public static void Validate(RecurrenceType recurrence, int? weekday, int? dayOfMonth, int? intervalDays)
    {
        switch (recurrence)
        {
            case RecurrenceType.Weekly when weekday is null or < 0 or > 6:
                throw new ValidationException("Для еженедельного повтора выберите день недели.");
            case RecurrenceType.Monthly when dayOfMonth is null or < 1 or > 31:
                throw new ValidationException("Для ежемесячного повтора укажите число от 1 до 31.");
            case RecurrenceType.EveryNDays when intervalDays is null or < 1 or > 365:
                throw new ValidationException("Интервал повтора: от 1 до 365 дней.");
        }
    }

    private static int DaysSinceAnchor(TodoTask task, DateOnly date) =>
        date.DayNumber - task.RecurrenceStartDate.DayNumber;

    private static int NormalizeWeekday(int weekday) => weekday;

    private static bool MatchesDayOfMonth(DateOnly date, int dayOfMonth)
    {
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        if (dayOfMonth <= daysInMonth)
            return date.Day == dayOfMonth;
        return date.Day == daysInMonth;
    }

    private static string GetWeeklyPeriodKey(TodoTask task, DateOnly date)
    {
        var target = task.Weekday ?? (int)date.DayOfWeek;
        var diff = ((int)date.DayOfWeek - target + 7) % 7;
        var occurrence = date.AddDays(-diff);
        return occurrence.ToString("yyyy-MM-dd");
    }
}
