using Barakah.InventoryService.Data;
using Barakah.InventoryService.Dtos;
using Barakah.InventoryService.Entities;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(InventoryDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InventoryItemResponse>> Create(CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var exists = await db.InventoryItems.AnyAsync(
            i => i.ProductId == request.ProductId && i.BranchId == request.BranchId, cancellationToken);
        if (exists)
        {
            return Conflict(new { error = "An inventory record for this product/branch already exists." });
        }

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId.Value,
            ProductId = request.ProductId,
            BranchId = request.BranchId,
            Quantity = request.Quantity,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            LastUpdated = DateTime.UtcNow,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetByProductAndBranch), new { productId = item.ProductId, branchId = item.BranchId }, ToResponse(item));
    }

    [HttpGet("{productId:guid}/{branchId:guid}")]
    public async Task<ActionResult<InventoryItemResponse>> GetByProductAndBranch(Guid productId, Guid branchId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.InventoryItems.SingleOrDefaultAsync(
            i => i.ProductId == productId && i.BranchId == branchId, cancellationToken);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpGet("branch/{branchId:guid}")]
    public async Task<ActionResult<List<InventoryItemResponse>>> ListByBranch(Guid branchId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var items = await db.InventoryItems.Where(i => i.BranchId == branchId).ToListAsync(cancellationToken);
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryItemResponse>>> ListLowStock(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var items = await db.InventoryItems.Where(i => i.Quantity < i.MinStockLevel).ToListAsync(cancellationToken);
        return Ok(items.Select(ToResponse));
    }

    [HttpPatch("{id:guid}/adjust")]
    public async Task<ActionResult<InventoryItemResponse>> Adjust(Guid id, AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var newQuantity = item.Quantity + request.Delta;
        if (newQuantity < 0)
        {
            return BadRequest(new { error = "Adjustment would result in negative stock." });
        }

        item.Quantity = newQuantity;
        item.UpdatedBy = request.UpdatedBy;
        item.LastUpdated = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    private static InventoryItemResponse ToResponse(InventoryItem item) => new(
        item.Id, item.ProductId, item.BranchId, item.Quantity, item.MinStockLevel, item.MaxStockLevel, item.LastUpdated);
}
