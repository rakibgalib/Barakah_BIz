using Barakah.CatalogService.Data;
using Barakah.CatalogService.Dtos;
using Barakah.CatalogService.Entities;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.CatalogService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(CatalogDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        if (await db.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            return Conflict(new { error = "A product with this SKU already exists." });
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId.Value,
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            BusinessType = request.BusinessType,
            Category = request.Category,
            Attributes = request.Attributes ?? "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> List(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var products = await db.Products.ToListAsync(cancellationToken);
        return Ok(products.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var product = await db.Products.FindAsync([id], cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<ProductResponse>> GetBySku(string sku, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var product = await db.Products.SingleOrDefaultAsync(p => p.Sku == sku, cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.Category = request.Category;
        product.Attributes = request.Attributes ?? product.Attributes;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.BasePrice,
        product.BusinessType, product.Category, product.Attributes, product.CreatedAt, product.UpdatedAt);
}
