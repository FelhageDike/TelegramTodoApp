using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgTodo.Bot;

public static class TelegramBotLifecycle
{
    public static string NormalizeWebhookPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/telegram/webhook";
        path = path.Trim();
        return path.StartsWith('/') ? path : "/" + path;
    }

    public static string BuildWebhookUrl(BotOptions options)
    {
        var baseUrl = options.WebhookPublicBaseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("Bot:WebhookPublicBaseUrl is required when DeliveryMode is Webhook (HTTPS URL without trailing slash).");

        return baseUrl + NormalizeWebhookPath(options.WebhookPath);
    }

    /// <summary>Регистрация команд, webhook или сброс webhook перед polling. Вызывать до <c>app.Run()</c>.</summary>
    public static async Task ConfigureAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var options = services.GetRequiredService<IOptions<BotOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.Token))
            return;

        var client = services.GetRequiredService<ITelegramBotClient>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TelegramBotLifecycle");

        User me;
        try
        {
            me = await client.GetMe(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot reach Telegram API during startup — bot will start; polling/webhook may retry");
            return;
        }

        await RegisterCommandsAsync(client, cancellationToken);

        if (BotDeliveryMode.IsWebhook(options.DeliveryMode))
        {
            if (string.IsNullOrWhiteSpace(options.WebhookSecretToken))
                throw new InvalidOperationException("Bot:WebhookSecretToken is required when DeliveryMode is Webhook (Telegram header X-Telegram-Bot-Api-Secret-Token).");

            var url = BuildWebhookUrl(options);
            await client.SetWebhook(
                url: url,
                allowedUpdates: new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery },
                secretToken: options.WebhookSecretToken,
                cancellationToken: cancellationToken);
            logger.LogInformation("Webhook set to {Url} for @{Username}", url, me.Username);
        }
        else
        {
            await client.DeleteWebhook(cancellationToken: cancellationToken);
            logger.LogInformation("Webhook cleared; long polling for @{Username}", me.Username);
        }
    }

    private static async Task RegisterCommandsAsync(ITelegramBotClient bot, CancellationToken ct)
    {
        var commands = new[]
        {
            new BotCommand { Command = "start", Description = "Старт и приложение" },
            new BotCommand { Command = "app", Description = "Открыть Mini App" },
            new BotCommand { Command = "today", Description = "Задачи на сегодня" },
            new BotCommand { Command = "balance", Description = "Баланс баллов" },
            new BotCommand { Command = "history", Description = "История операций" },
            new BotCommand { Command = "groups", Description = "Мои группы" },
            new BotCommand { Command = "newgroup", Description = "Создать группу" },
            new BotCommand { Command = "join", Description = "Вступить по коду" },
            new BotCommand { Command = "newtask", Description = "Быстрая задача" },
            new BotCommand { Command = "context", Description = "Контекст: personal" },
            new BotCommand { Command = "help", Description = "Справка" }
        };

        await bot.SetMyCommands(commands, cancellationToken: ct);
    }
}
