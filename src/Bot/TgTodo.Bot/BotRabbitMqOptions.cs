namespace TgTodo.Bot;

/// <summary>Настройки RabbitMQ для очереди Telegram-апдейтов (секция <c>Bot:RabbitMq</c>).</summary>
public sealed class BotRabbitMqOptions
{
    /// <summary>Имя хоста или сервиса Docker, например <c>rabbitmq</c>. Пусто — используется in-memory канал.</summary>
    public string HostName { get; set; } = "";

    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Vhost; пустая строка трактуется как <c>/</c>.</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Durable-очередь для JSON апдейтов Telegram.</summary>
    public string QueueName { get; set; } = "tgbot.telegram.updates";

    /// <summary>Сколько параллельных consumer-каналов поднимается (каждый с QoS prefetch 1).</summary>
    public int MaxConsumerChannels { get; set; } = 16;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(HostName);
}
