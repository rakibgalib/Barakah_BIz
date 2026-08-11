using Barakah.PaymentService.Data;
using Barakah.PaymentService.Dtos;
using Barakah.PaymentService.Entities;
using Barakah.PaymentService.Services;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(PaymentDbContext db, ITenantContext tenant, OrderClient orderClient) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId.Value,
            OrderId = request.OrderId,
            Amount = request.Amount,
            Method = request.Method,
            Status = "completed",
            TransactionReference = request.TransactionReference,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        // Best-effort: the payment record is the source of truth regardless of whether this
        // sync succeeds. A failed push here doesn't roll back the payment — Order Service would
        // need to be reconciled separately (future work; no outbox/retry yet, same limitation as
        // Barakah.EventBus).
        await orderClient.UpdatePaymentStatusAsync(payment.OrderId, "paid", tenant.Subdomain!, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, ToResponse(payment));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var payment = await db.Payments.FindAsync([id], cancellationToken);
        return payment is null ? NotFound() : Ok(ToResponse(payment));
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<List<PaymentResponse>>> ListByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var payments = await db.Payments.Where(p => p.OrderId == orderId).ToListAsync(cancellationToken);
        return Ok(payments.Select(ToResponse));
    }

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.Id, payment.OrderId, payment.Amount, payment.Method, payment.Status, payment.TransactionReference, payment.CreatedAt);
}
