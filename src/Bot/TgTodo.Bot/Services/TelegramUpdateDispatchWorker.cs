using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace TgTodo.Bot.Services;

/// <summary>
/// Запускает цикл обработки апдейтов (<see cref="ITelegramUpdateIngress.RunDispatchLoopAsync"/>).
/// </summary>
public sealed class TelegramUpdateDispatchWorker : BackgroundService
{
    private readonly ILogger<TelegramUpdateDispatchWorker> _logger;
    private readonly ITelegramBotClient _bot;
    private readonly BotUpdateHandler _handler;
    private readonly ITelegramUpdateIngress _ingress;
    private readonly BotOptions _options;

    public TelegramUpdateDispatchWorker(
        ILogger<TelegramUpdateDispatchWorker> logger,
        ITelegramBotClient bot,
        BotUpdateHandler handler,
        ITelegramUpdateIngress ingress,
        IOptions<BotOptions> options)
    {
        _logger = logger;
        _bot = bot;
        _handler = handler;
        _ingress = ingress;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("BOT_TOKEN empty — update dispatcher idle");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation(
            "Update dispatcher started (transport: {Transport})",
            _options.RabbitMq.IsEnabled ? "RabbitMQ" : "in-memory channel");

        await _ingress.RunDispatchLoopAsync(_bot, _handler, stoppingToken).ConfigureAwait(false);
    }
}
