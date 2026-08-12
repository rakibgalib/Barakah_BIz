using Barakah.Persistence;
using Barakah.RestaurantService.Data;
using Barakah.RestaurantService.Dtos;
using Barakah.RestaurantService.Entities;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barakah.RestaurantService.Controllers;

[ApiController]
[Route("api/menu-items")]
public class MenuItemsController(RestaurantDbContext db, ITenantContext tenant, IOptions<RestaurantOptions> options) : ControllerBase
{
    private readonly RestaurantOptions _options = options.Value;

    [HttpPost]
    public async Task<IActionResult> Create(CreateMenuItemRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId!.Value,
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            Category = request.Category,
            AllergenTags = request.AllergenTags,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.MenuItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var items = await db.MenuItems.OrderBy(m => m.Category).ThenBy(m => m.Name).ToListAsync(cancellationToken);
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMenuItemRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (item is null) return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.BasePrice = request.BasePrice;
        item.Category = request.Category;
        item.AllergenTags = request.AllergenTags;
        item.IsAvailable = request.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (item is null) return NotFound();
        db.MenuItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Allergy Management: filter menu items that do NOT contain any of the given allergens.
    [HttpGet("allergen-free")]
    public async Task<IActionResult> AllergenFree([FromQuery] string exclude, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var excluded = (exclude ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.ToLowerInvariant())
            .ToHashSet();

        var items = await db.MenuItems.Where(m => m.IsAvailable).ToListAsync(cancellationToken);
        var safe = items.Where(m =>
        {
            var tags = (m.AllergenTags ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(a => a.ToLowerInvariant());
            return !tags.Any(excluded.Contains);
        });
        return Ok(safe.Select(ToResponse));
    }

    // Dynamic Pricing: rule-based multipliers from RestaurantOptions, not AI/demand forecasting.
    [HttpGet("{id:guid}/price")]
    public async Task<IActionResult> GetCurrentPrice(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var (multiplier, tier) = ComputePricingMultiplier();
        var currentPrice = Math.Round(item.BasePrice * multiplier, 2);
        return Ok(new PricedMenuItemResponse(item.Id, item.Name, item.BasePrice, currentPrice, tier));
    }

    private (decimal multiplier, string tier) ComputePricingMultiplier()
    {
        if (!_options.DynamicPricingEnabled) return (1.0m, "Standard");
        if (_options.IsRamadanPeriod) return (_options.RamadanPricing, "Ramadan");

        var hourUtc = DateTime.UtcNow.Hour;
        var isPeak = hourUtc >= _options.PeakHourStartUtc && hourUtc < _options.PeakHourEndUtc;
        return isPeak
            ? (_options.PeakHourPricing, "Peak")
            : (1.0m - _options.OffPeakDiscount, "OffPeak");
    }

    private static MenuItemResponse ToResponse(MenuItem m) => new(
        m.Id, m.TenantId, m.Name, m.Description, m.BasePrice, m.Category, m.AllergenTags,
        m.IsAvailable, m.CreatedAt, m.UpdatedAt);
}
