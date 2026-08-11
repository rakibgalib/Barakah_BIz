using System.Text.Json;
using Barakah.EventBus;
using Barakah.NotificationService.Data;
using Barakah.NotificationService.Entities;
using Barakah.Persistence;

namespace Barakah.NotificationService.Consumers;

/// <summary>
/// Consumes "order-created" events published by Order Service and records a simulated
/// notification (no real email/SMS provider call — see NotificationsController for the same
/// simplification on the manual/test-trigger path). Best-effort: any failure is logged, never
/// allowed to crash the consumer loop.
/// </summary>
public class OrderCreatedConsumer(
    IEventSubscriber subscriber,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await subscriber.SubscribeAsync("order-created", HandleAsync, stoppingToken);
    }

    private async Task HandleAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(json);
            if (evt is null)
            {
                logger.LogWarning("Received unparseable order-created message: {Payload}", json);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            await db.UseTenantSchemaAsync(evt.TenantId, cancellationToken);

            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = evt.TenantId,
                OrderId = evt.OrderId,
                Channel = "email",
                Recipient = $"tenant-{evt.TenantId}@notifications.barakah.local",
                Message = $"Order {evt.OrderNumber} confirmed - total {evt.Total:C}",
                Status = "Sent",
                CreatedAt = now,
                SentAt = now,
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Notification sent for order {OrderNumber}", evt.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle order-created message: {Payload}", json);
        }
    }
}
