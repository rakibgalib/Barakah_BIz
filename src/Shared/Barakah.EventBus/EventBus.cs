using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Barakah.EventBus;

public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}

/// <summary>
/// Published by Order Service after an order is created and stock has been deducted.
/// Producer-only for now — no consumer exists yet (Notification/Analytics services aren't
/// built). Topic: "order-created".
/// </summary>
public record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid BranchId { get; init; }
    public required string OrderNumber { get; init; }
    public required decimal Total { get; init; }
}

public class KafkaEventBusOptions
{
    public required string BootstrapServers { get; set; }
}

/// <summary>
/// Publishing is best-effort: a Kafka outage should not take down the publishing service's
/// primary request flow (e.g. user registration). Failures are logged, not thrown. This is a
/// known simplification for the Foundation phase — no outbox/retry pattern yet.
/// </summary>
public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(KafkaEventBusOptions options, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;
        var config = new ProducerConfig { BootstrapServers = options.BootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        try
        {
            var payload = JsonSerializer.Serialize(@event, @event.GetType());
            await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = @event.Id.ToString(), Value = payload },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {EventType} to topic {Topic}", typeof(TEvent).Name, topic);
        }
    }

    public void Dispose() => _producer.Dispose();
}

public class KafkaConsumerOptions
{
    public required string BootstrapServers { get; set; }
    public required string GroupId { get; set; }
}

public interface IEventSubscriber : IDisposable
{
    Task SubscribeAsync(string topic, Func<string, CancellationToken, Task> handler, CancellationToken cancellationToken);
}

/// <summary>
/// Consumption is best-effort, matching the publish-side philosophy in <see cref="KafkaEventBus"/>:
/// a bad or unexpected message should not crash the consuming service. Consume errors are logged
/// and skipped; handler errors are logged and swallowed so the loop keeps running.
/// </summary>
public class KafkaEventSubscriber : IEventSubscriber
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaEventSubscriber> _logger;

    public KafkaEventSubscriber(KafkaConsumerOptions options, ILogger<KafkaEventSubscriber> logger)
    {
        _logger = logger;
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public async Task SubscribeAsync(string topic, Func<string, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        _consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning(ex, "Failed to consume message from topic {Topic}", topic);
                continue;
            }

            if (result is null)
            {
                continue;
            }

            try
            {
                await handler(result.Message.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler failed for message from topic {Topic}", topic);
            }
        }
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
