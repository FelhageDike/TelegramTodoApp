using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgTodo.Bot.Services;

namespace TgTodo.Bot;

public sealed class TelegramBotWorker : BackgroundService
{
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly BotOptions _options;
    private readonly BotUpdateHandler _handler;
    private ITelegramBotClient? _bot;

    public TelegramBotWorker(
        ILogger<TelegramBotWorker> logger,
        IOptions<BotOptions> options,
        BotUpdateHandler handler)
    {
        _logger = logger;
        _options = options.Value;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("BOT_TOKEN is empty — bot idle until token is set in deploy/.env");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _bot = new TelegramBotClient(_options.Token);

        var me = await WaitForTelegramAsync(stoppingToken);
        if (me is null)
        {
            _logger.LogError("Cannot reach api.telegram.org — retrying every 30s (check firewall/DNS)");
            await RetryConnectLoopAsync(stoppingToken);
            return;
        }

        _logger.LogInformation("Bot started as @{Username}", me.Username);
        await RegisterCommandsAsync(stoppingToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery }
        };

        _bot.StartReceiving(
            (client, update, ct) => _handler.HandleAsync(client, update, ct),
            (_, ex, _) =>
            {
                _logger.LogError(ex, "Telegram polling error");
                return Task.CompletedTask;
            },
            receiverOptions,
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            _handler.CleanupDrafts();
        }
    }

    private async Task<User?> WaitForTelegramAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (_bot is null) return null;
                return await _bot.GetMe(ct);
            }
            catch (Exception ex) when (attempt < 5)
            {
                _logger.LogWarning(ex, "Telegram API unreachable (attempt {Attempt}/5)", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3 * attempt), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram API unreachable (attempt {Attempt}/5)", attempt);
            }
        }

        return null;
    }

    private async Task RetryConnectLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            var me = await WaitForTelegramAsync(stoppingToken);
            if (me is null || _bot is null)
                continue;

            _logger.LogInformation("Bot connected as @{Username}", me.Username);
            await RegisterCommandsAsync(stoppingToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery }
            };

            _bot.StartReceiving(
                (client, update, ct) => _handler.HandleAsync(client, update, ct),
                (_, ex, _) =>
                {
                    _logger.LogError(ex, "Telegram polling error");
                    return Task.CompletedTask;
                },
                receiverOptions,
                stoppingToken);
            return;
        }
    }

    private async Task RegisterCommandsAsync(CancellationToken ct)
    {
        if (_bot is null) return;

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

        await _bot.SetMyCommands(commands, cancellationToken: ct);
    }
}
