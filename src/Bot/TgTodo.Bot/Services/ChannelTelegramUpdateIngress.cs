using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgTodo.Bot;

namespace TgTodo.Bot.Services;

/// <summary>In-memory очередь (режим без RabbitMQ).</summary>
public sealed class ChannelTelegramUpdateIngress : ITelegramUpdateIngress
{
    private readonly UpdateIngestQueue _queue;
    private readonly ILogger<ChannelTelegramUpdateIngress> _logger;
    private readonly BotOptions _options;

    public ChannelTelegramUpdateIngress(
        UpdateIngestQueue queue,
        IOptions<BotOptions> options,
        ILogger<ChannelTelegramUpdateIngress> logger)
    {
        _queue = queue;
        _logger = logger;
        _options = options.Value;
    }

    public ValueTask<bool> PublishAsync(Update update, UpdateIngressPublishMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == UpdateIngressPublishMode.WebhookNoWait)
            return ValueTask.FromResult(_queue.TryEnqueue(update));

        return PublishPollingAsync(update, cancellationToken);
    }

    private async ValueTask<bool> PublishPollingAsync(Update update, CancellationToken cancellationToken)
    {
        await _queue.EnqueueWithBackpressureAsync(update, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task RunDispatchLoopAsync(ITelegramBotClient bot, BotUpdateHandler handler, CancellationToken cancellationToken)
    {
        var parallelism = Math.Clamp(_options.MaxParallelUpdateHandlers, 1, 512);
        _logger.LogInformation("Update dispatch: in-memory channel (parallelism {Parallelism})", parallelism);

        await Parallel.ForEachAsync(
            _queue.Reader.ReadAllAsync(cancellationToken),
            new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken },
            async (update, ct) =>
            {
                try
                {
                    await handler.HandleAsync(bot, update, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing update {UpdateId}", update.Id);
                }
            }).ConfigureAwait(false);
    }
}
