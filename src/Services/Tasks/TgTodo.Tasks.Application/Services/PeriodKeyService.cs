using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Services;

public static class PeriodKeyService
{
    public static string GetCurrentPeriodKey(TodoTask task, DateOnly date) =>
        RecurrenceSchedule.GetPeriodKey(task, date);
}
