namespace TgTodo.Bff.Auth;

public class UserContext
{
    public Guid UserId { get; set; }
    public long TelegramId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
