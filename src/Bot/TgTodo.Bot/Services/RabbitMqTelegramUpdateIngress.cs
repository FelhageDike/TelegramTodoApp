using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgTodo.Bot.Services;

/// <summary>
/// Очередь апдейтов в RabbitMQ: публикация из webhook/polling, несколько consumer-каналов (prefetch 1) для параллельной обработки.
/// </summary>
public sealed class RabbitMqTelegramUpdateIngress : ITelegramUpdateIngress, IDisposable
{
    private readonly ILogger<RabbitMqTelegramUpdateIngress> _logger;
    private readonly BotRabbitMqOptions _rmq;

    private readonly SemaphoreSlim _initAsync = new(1, 1);
    private readonly object _sync = new();

    private IConnection? _connection;
    private IModel? _publishChannel;
    private int _disposed;

    public RabbitMqTelegramUpdateIngress(
        IOptions<BotOptions> botOptions,
        ILogger<RabbitMqTelegramUpdateIngress> logger)
    {
        _logger = logger;
        _rmq = botOptions.Value.RabbitMq;
    }

    public async ValueTask<bool> PublishAsync(Update update, UpdateIngressPublishMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(update, JsonBotAPI.Options));
        var maxAttempts = mode == UpdateIngressPublishMode.PollingWait ? 60 : 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await EnsurePublishInfrastructureAsync(cancellationToken).ConfigureAwait(false);

                lock (_sync)
                {
                    if (_publishChannel is not { IsOpen: true })
                        throw new InvalidOperationException("Publish channel not open");

                    var props = _publishChannel.CreateBasicProperties();
                    props.Persistent = true;
                    props.ContentType = "application/json";
                    _publishChannel.BasicPublish("", _rmq.QueueName, false, props, body);
                }

                return true;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "RabbitMQ publish failed ({Attempt}/{Max})", attempt, maxAttempts);
                if (attempt == maxAttempts)
                    return false;
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken).ConfigureAwait(false);
                await ResetInfrastructureAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    public async Task RunDispatchLoopAsync(ITelegramBotClient bot, BotUpdateHandler handler, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var workers = Math.Clamp(_rmq.MaxConsumerChannels, 1, 64);
        _logger.LogInformation(
            "RabbitMQ dispatch: {Workers} consumer channel(s), queue {Queue}, host {Host}:{Port}",
            workers, _rmq.QueueName, _rmq.HostName, _rmq.Port);

        await EnsurePublishInfrastructureAsync(cancellationToken).ConfigureAwait(false);

        var tasks = Enumerable.Range(0, workers)
            .Select(i => RunConsumerAsync(bot, handler, i, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunConsumerAsync(ITelegramBotClient bot, BotUpdateHandler handler, int workerId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            IModel? channel = null;
            try
            {
                await EnsurePublishInfrastructureAsync(ct).ConfigureAwait(false);

                lock (_sync)
                {
                    if (_connection is not { IsOpen: true })
                        throw new InvalidOperationException("No RabbitMQ connection");
                    channel = _connection.CreateModel();
                }

                channel.QueueDeclare(_rmq.QueueName, true, false, false, null);
                channel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var update = JsonSerializer.Deserialize<Update>(json, JsonBotAPI.Options);
                        if (update is not null)
                            await handler.HandleAsync(bot, update, ct).ConfigureAwait(false);

                        if (channel.IsOpen)
                            channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        if (channel.IsOpen)
                            channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "RabbitMQ worker {WorkerId} failed handling update", workerId);
                        if (channel.IsOpen)
                            channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                    }
                };

                channel.BasicConsume(_rmq.QueueName, false, consumer);
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ worker {WorkerId} will reconnect", workerId);
                await Task.Delay(TimeSpan.FromSeconds(3), ct).ConfigureAwait(false);
            }
            finally
            {
                if (channel is not null)
                {
                    try
                    {
                        if (channel.IsOpen)
                            channel.Close();
                        channel.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
    }

    private async Task EnsurePublishInfrastructureAsync(CancellationToken ct)
    {
        await _initAsync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            lock (_sync)
            {
                if (_connection is { IsOpen: true } && _publishChannel is { IsOpen: true })
                    return;

                CloseInfrastructureUnsafe();

                var factory = new ConnectionFactory
                {
                    HostName = _rmq.HostName,
                    Port = _rmq.Port,
                    UserName = _rmq.UserName,
                    Password = _rmq.Password,
                    VirtualHost = string.IsNullOrEmpty(_rmq.VirtualHost) ? "/" : _rmq.VirtualHost,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    TopologyRecoveryEnabled = true
                };

                _connection = factory.CreateConnection("tgbot-updates");
                _publishChannel = _connection.CreateModel();
                _publishChannel.QueueDeclare(_rmq.QueueName, true, false, false, null);
                _logger.LogInformation("RabbitMQ connection open, queue {Queue}", _rmq.QueueName);
            }
        }
        finally
        {
            _initAsync.Release();
        }
    }

    private async Task ResetInfrastructureAsync(CancellationToken ct)
    {
        await _initAsync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            lock (_sync)
                CloseInfrastructureUnsafe();
        }
        finally
        {
            _initAsync.Release();
        }
    }

    private void CloseInfrastructureUnsafe()
    {
        try
        {
            _publishChannel?.Close();
            _publishChannel?.Dispose();
        }
        catch
        {
            // ignore
        }

        _publishChannel = null;

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch
        {
            // ignore
        }

        _connection = null;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _initAsync.Wait(TimeSpan.FromSeconds(10));
            try
            {
                lock (_sync)
                    CloseInfrastructureUnsafe();
            }
            finally
            {
                _initAsync.Release();
            }
        }
        catch
        {
            lock (_sync)
                CloseInfrastructureUnsafe();
        }

        _initAsync.Dispose();
    }
}
