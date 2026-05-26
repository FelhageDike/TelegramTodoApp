using Microsoft.Extensions.Options;

namespace TgTodo.Bot.Services;

/// <summary>Периодически чистит inline-черновики на BFF.</summary>
public sealed class DraftCleanupWorker : BackgroundService
{
    private readonly ILogger<DraftCleanupWorker> _logger;
    private readonly BotUpdateHandler _handler;
    private readonly BotOptions _options;

    public DraftCleanupWorker(
        ILogger<DraftCleanupWorker> logger,
        BotUpdateHandler handler,
        IOptions<BotOptions> options)
    {
        _logger = logger;
        _handler = handler;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            try
            {
                await _handler.CleanupDraftsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Draft prune failed");
            }
        }
    }
}
