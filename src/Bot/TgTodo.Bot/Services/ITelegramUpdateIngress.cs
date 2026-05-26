using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgTodo.Bot.Services;

public enum UpdateIngressPublishMode
{
    /// <summary>Не ждать бесконечно: in-memory — 503 при переполнении; RabbitMQ — ошибка публикации.</summary>
    WebhookNoWait,

    /// <summary>Гарантировать приём: in-memory — блокировка; RabbitMQ — повторные попытки публикации.</summary>
    PollingWait
}

/// <summary>Приём апдейтов (webhook / polling) и цикл их обработки.</summary>
public interface ITelegramUpdateIngress
{
    /// <returns><c>true</c>, если сообщение принято в очередь (или опубликовано в RabbitMQ).</returns>
    ValueTask<bool> PublishAsync(Update update, UpdateIngressPublishMode mode, CancellationToken cancellationToken = default);

    Task RunDispatchLoopAsync(ITelegramBotClient bot, BotUpdateHandler handler, CancellationToken cancellationToken);
}
