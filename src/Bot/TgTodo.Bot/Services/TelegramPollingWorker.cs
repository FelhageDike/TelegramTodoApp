using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgTodo.Bot.Services;

/// <summary>Long polling: только приём апдейтов в очередь (<see cref="ITelegramUpdateIngress"/>).</summary>
public sealed class TelegramPollingWorker : BackgroundService
{
    private readonly ILogger<TelegramPollingWorker> _logger;
    private readonly ITelegramBotClient _bot;
    private readonly BotOptions _options;
    private readonly ITelegramUpdateIngress _ingress;

    public TelegramPollingWorker(
        ILogger<TelegramPollingWorker> logger,
        ITelegramBotClient bot,
        IOptions<BotOptions> options,
        ITelegramUpdateIngress ingress)
    {
        _logger = logger;
        _bot = bot;
        _options = options.Value;
        _ingress = ingress;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("BOT_TOKEN empty — polling idle");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        if (BotDeliveryMode.IsWebhook(_options.DeliveryMode))
        {
            _logger.LogInformation("Delivery mode is Webhook — polling worker idle");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        if (!await WaitForTelegramAsync(stoppingToken))
        {
            _logger.LogError("Telegram API unreachable — polling disabled");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation("Long polling enabled — updates go to ingress queue");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery }
        };

        _bot.StartReceiving(
            async (_, update, ct) =>
            {
                try
                {
                    await _ingress.PublishAsync(update, UpdateIngressPublishMode.PollingWait, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue update {UpdateId}", update.Id);
                }
            },
            (_, ex, _) =>
            {
                _logger.LogError(ex, "Telegram polling error");
                return Task.CompletedTask;
            },
            receiverOptions,
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
    }

    private async Task<bool> WaitForTelegramAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await _bot.GetMe(ct);
                return true;
            }
            catch (Exception ex) when (attempt < 10)
            {
                _logger.LogWarning(ex, "Telegram API unreachable (attempt {Attempt}/10)", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 3 * attempt)), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram API unreachable (attempt {Attempt}/10)", attempt);
            }
        }

        return false;
    }
}
