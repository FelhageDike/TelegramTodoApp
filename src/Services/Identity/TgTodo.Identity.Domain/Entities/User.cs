using TgTodo.BuildingBlocks.Domain;

namespace TgTodo.Identity.Domain.Entities;

public class User : AuditableEntity
{
    public long TelegramId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "UTC";

    private User() { }

    public static User Create(long telegramId, string displayName, string timezone)
    {
        return new User
        {
            TelegramId = telegramId,
            DisplayName = displayName,
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone
        };
    }

    public void Update(string displayName, string timezone)
    {
        DisplayName = displayName;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? Timezone : timezone;
        MarkUpdated();
    }
}
