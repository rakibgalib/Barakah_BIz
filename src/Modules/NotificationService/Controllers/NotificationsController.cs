using Barakah.NotificationService.Data;
using Barakah.NotificationService.Dtos;
using Barakah.NotificationService.Entities;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> List(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var notifications = await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(notifications.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var notification = await db.Notifications.FindAsync([id], cancellationToken);
        return notification is null ? NotFound() : Ok(ToResponse(notification));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationResponse>> Create(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        // Manual/test trigger — simulates an immediate send. There is no real email/SMS
        // provider call here, matching how PaymentService is a ledger rather than a real
        // Stripe integration.
        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId.Value,
            OrderId = request.OrderId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Message = request.Message,
            Status = "Sent",
            CreatedAt = now,
            SentAt = now,
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = notification.Id }, ToResponse(notification));
    }

    private static NotificationResponse ToResponse(Notification notification) => new(
        notification.Id,
        notification.TenantId,
        notification.OrderId,
        notification.Channel,
        notification.Recipient,
        notification.Message,
        notification.Status,
        notification.CreatedAt,
        notification.SentAt);
}
