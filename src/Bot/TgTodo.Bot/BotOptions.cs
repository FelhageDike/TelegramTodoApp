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

    /// <summary>Polling (по умолчанию) или Webhook — см. <see cref="BotDeliveryMode"/>.</summary>
    public string DeliveryMode { get; set; } = BotDeliveryMode.Polling;

    /// <summary>HTTPS-URL без пути, например https://bot.example.com — для SetWebhook.</summary>
    public string? WebhookPublicBaseUrl { get; set; }

    /// <summary>Путь на вашем хосте, должен совпадать с URL после домена (по умолчанию /telegram/webhook).</summary>
    public string WebhookPath { get; set; } = "/telegram/webhook";

    /// <summary>Секрет для заголовка X-Telegram-Bot-Api-Secret-Token (обязателен в режиме Webhook).</summary>
    public string? WebhookSecretToken { get; set; }

    /// <summary>Размер очереди входящих апдейтов (между приёмом и обработкой).</summary>
    public int UpdateQueueCapacity { get; set; } = 2000;

    /// <summary>Максимум одновременных обработчиков одного апдейта (HTTP к BFF и т.д.).</summary>
    public int MaxParallelUpdateHandlers { get; set; } = 16;

    public BotRabbitMqOptions RabbitMq { get; set; } = new();
}
