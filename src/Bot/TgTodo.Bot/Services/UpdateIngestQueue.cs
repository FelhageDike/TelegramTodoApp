using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace TgTodo.Bot.Services;

/// <summary>
/// Очередь входящих <see cref="Update"/> между HTTP/polling и пулом обработчиков.
/// Webhook: при переполнении не ждём — 503 (Telegram повторит). Polling: ждём место.
/// </summary>
public sealed class UpdateIngestQueue
{
    private readonly Channel<Update> _channel;
    private readonly ILogger<UpdateIngestQueue> _logger;

    public UpdateIngestQueue(IOptions<BotOptions> options, ILogger<UpdateIngestQueue> logger)
    {
        _logger = logger;
        var cap = Math.Clamp(options.Value.UpdateQueueCapacity, 64, 100_000);
        _channel = Channel.CreateBounded<Update>(new BoundedChannelOptions(cap)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ChannelReader<Update> Reader => _channel.Reader;

    /// <summary>Для webhook: не блокируем поток приёмника.</summary>
    public bool TryEnqueue(Update update)
    {
        if (_channel.Writer.TryWrite(update))
            return true;
        _logger.LogWarning("Update queue full; rejecting update {UpdateId} (webhook will retry)", update.Id);
        return false;
    }

    /// <summary>Для long polling: не теряем апдейт — ждём место в очереди.</summary>
    public async ValueTask EnqueueWithBackpressureAsync(Update update, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(update, cancellationToken);
}
