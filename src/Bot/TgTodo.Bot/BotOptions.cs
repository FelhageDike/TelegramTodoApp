namespace TgTodo.Bot;

public sealed class BotOptions
{
    public const string SectionName = "Bot";

    public string Token { get; set; } = "";
    public string BffBaseUrl { get; set; } = "http://localhost:5000";
    public string MiniAppUrl { get; set; } = "http://localhost:5000/";
    public string InternalKey { get; set; } = "dev-bot-key";
    public int DefaultPoints { get; set; } = 10;
    public int DraftTtlMinutes { get; set; } = 15;
}
