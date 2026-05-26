using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgTodo.Contracts.Events;
using TgTodo.Tasks.Infrastructure.Persistence;

namespace TgTodo.Tasks.Infrastructure.Outbox;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromSeconds(15);
            try
            {
                var pending = await ProcessBatchAsync(stoppingToken);
                delay = pending > 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(15);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox dispatch failed, will retry");
                delay = TimeSpan.FromSeconds(5);
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == nameof(TaskCompletedIntegrationEvent))
                {
                    var evt = JsonSerializer.Deserialize<TaskCompletedIntegrationEvent>(message.Content)!;
                    await publishEndpoint.Publish(evt, ct);
                }

                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);

        return messages.Count;
    }
}
