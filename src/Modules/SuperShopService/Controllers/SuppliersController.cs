using Barakah.Persistence;
using Barakah.SuperShopService.Data;
using Barakah.SuperShopService.Dtos;
using Barakah.SuperShopService.Entities;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.SuperShopService.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController(SuperShopDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId!.Value,
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, ToResponse(supplier));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var suppliers = await db.Suppliers.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return Ok(suppliers.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return supplier is null ? NotFound() : Ok(ToResponse(supplier));
    }

    [HttpPatch("{id:guid}/rating")]
    public async Task<IActionResult> UpdateRating(Guid id, UpdateSupplierRatingRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null) return NotFound();
        if (request.Rating is < 0 or > 5) return BadRequest(new { error = "Rating must be between 0 and 5." });

        supplier.Rating = request.Rating;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(supplier));
    }

    private static SupplierResponse ToResponse(Supplier s) => new(
        s.Id, s.TenantId, s.Name, s.ContactEmail, s.ContactPhone, s.Rating, s.IsActive, s.CreatedAt);
}
