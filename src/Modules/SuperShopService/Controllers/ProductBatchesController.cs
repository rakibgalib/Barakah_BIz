using Barakah.Persistence;
using Barakah.SuperShopService.Data;
using Barakah.SuperShopService.Dtos;
using Barakah.SuperShopService.Entities;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barakah.SuperShopService.Controllers;

[ApiController]
[Route("api/product-batches")]
public class ProductBatchesController(SuperShopDbContext db, ITenantContext tenant, IOptions<SuperShopOptions> options) : ControllerBase
{
    private readonly SuperShopOptions _options = options.Value;

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductBatchRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var batch = new ProductBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId!.Value,
            ProductId = request.ProductId,
            BatchNumber = request.BatchNumber,
            Quantity = request.Quantity,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        db.ProductBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, ToResponse(batch));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var batch = await db.ProductBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return batch is null ? NotFound() : Ok(ToResponse(batch));
    }

    // Expiry Management: batches expiring within the configured warning window.
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var cutoff = DateTime.UtcNow.AddDays(_options.ExpiryWarningDays);
        var batches = await db.ProductBatches
            .Where(b => b.ExpiresAt <= cutoff)
            .OrderBy(b => b.ExpiresAt)
            .ToListAsync(cancellationToken);
        return Ok(batches.Select(ToResponse));
    }

    private static ProductBatchResponse ToResponse(ProductBatch b) => new(
        b.Id, b.TenantId, b.ProductId, b.BatchNumber, b.Quantity, b.ExpiresAt, b.CreatedAt);
}

// Bulk Discounts: stateless rule-based calculation, no entity needed.
[ApiController]
[Route("api/pricing")]
public class PricingController(IOptions<SuperShopOptions> options) : ControllerBase
{
    private readonly SuperShopOptions _options = options.Value;

    [HttpPost("bulk-discount")]
    public IActionResult CalculateBulkDiscount(BulkDiscountRequest request)
    {
        var applies = request.Quantity >= _options.BulkDiscountThreshold;
        var discountPercentage = applies ? _options.BulkDiscountPercentage : 0m;
        var totalPrice = Math.Round(request.UnitPrice * request.Quantity * (1 - discountPercentage), 2);
        return Ok(new BulkDiscountResponse(request.UnitPrice, request.Quantity, applies, discountPercentage, totalPrice));
    }
}
