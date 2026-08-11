using Barakah.EventBus;
using Barakah.OrderService.Data;
using Barakah.OrderService.Dtos;
using Barakah.OrderService.Entities;
using Barakah.OrderService.Services;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    OrderDbContext db,
    ITenantContext tenant,
    CatalogClient catalogClient,
    InventoryClient inventoryClient,
    IEventBus eventBus) : ControllerBase
{
    private const string OrderCreatedTopic = "order-created";

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "An order needs at least one item." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        // Phase 1: validate every line item (price + available stock) before writing anything.
        // No reservation/locking here — a concurrent order for the same stock can still race
        // between this check and the adjust call below. Acceptable simplification for this
        // slice (see plan); a real reservation system is future work.
        var validated = new List<(ProductLookupResponse Product, InventoryLookupResponse Inventory, int Quantity)>();
        foreach (var item in request.Items)
        {
            var product = await catalogClient.GetProductAsync(item.ProductId, tenant.Subdomain!, cancellationToken);
            if (product is null)
            {
                return BadRequest(new { error = $"Product {item.ProductId} not found." });
            }

            var inventory = await inventoryClient.GetInventoryAsync(item.ProductId, request.BranchId, tenant.Subdomain!, cancellationToken);
            if (inventory is null)
            {
                return BadRequest(new { error = $"No inventory record for product {item.ProductId} at branch {request.BranchId}." });
            }

            if (inventory.Quantity < item.Quantity)
            {
                return BadRequest(new { error = $"Insufficient stock for product {item.ProductId}: requested {item.Quantity}, available {inventory.Quantity}." });
            }

            validated.Add((product, inventory, item.Quantity));
        }

        // Phase 2: deduct stock. Each item was just validated above, so failures here are an
        // unexpected race rather than the normal case.
        foreach (var (_, inventory, quantity) in validated)
        {
            var adjusted = await inventoryClient.AdjustAsync(inventory.Id, -quantity, tenant.Subdomain!, cancellationToken);
            if (!adjusted)
            {
                return Conflict(new { error = $"Failed to reserve stock for product {inventory.ProductId} — it may have just sold out. No order was created." });
            }
        }

        // Phase 3: create the order.
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId.Value,
            BranchId = request.BranchId,
            OrderNumber = $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CustomerId = request.CustomerId,
            Status = "confirmed",
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "pending",
            CreatedBy = request.CreatedBy,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (product, _, quantity) in validated)
        {
            var totalPrice = product.BasePrice * quantity;
            order.Subtotal += totalPrice;
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.TenantId.Value,
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.BasePrice,
                TotalPrice = totalPrice,
                CreatedAt = now,
            });
        }
        order.Total = order.Subtotal - order.Discount + order.Tax;

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        // Phase 4: publish, best-effort — KafkaEventBus already swallows publish failures.
        await eventBus.PublishAsync(OrderCreatedTopic, new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            TenantId = order.TenantId,
            BranchId = order.BranchId,
            OrderNumber = order.OrderNumber,
            Total = order.Total,
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        return order is null ? NotFound() : Ok(ToResponse(order));
    }

    [HttpGet("branch/{branchId:guid}")]
    public async Task<ActionResult<List<OrderResponse>>> ListByBranch(Guid branchId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var orders = await db.Orders.Include(o => o.Items).Where(o => o.BranchId == branchId).ToListAsync(cancellationToken);
        return Ok(orders.Select(ToResponse));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(order));
    }

    [HttpPatch("{id:guid}/payment-status")]
    public async Task<ActionResult<OrderResponse>> UpdatePaymentStatus(Guid id, UpdateOrderPaymentStatusRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        order.PaymentStatus = request.PaymentStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id, order.OrderNumber, order.BranchId, order.CustomerId, order.Status,
        order.Subtotal, order.Discount, order.Tax, order.Total, order.PaymentMethod, order.PaymentStatus,
        order.CreatedAt,
        order.Items.Select(i => new OrderItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.TotalPrice, i.Discount)).ToList());
}
