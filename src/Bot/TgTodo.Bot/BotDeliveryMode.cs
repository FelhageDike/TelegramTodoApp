namespace TgTodo.Bot;

public static class BotDeliveryMode
{
    public const string Polling = "Polling";
    public const string Webhook = "Webhook";

    public static bool IsWebhook(string? mode) =>
        string.Equals(mode, Webhook, StringComparison.OrdinalIgnoreCase);
}
