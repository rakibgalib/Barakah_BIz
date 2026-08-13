using Barakah.ClothingService.Data;
using Barakah.ClothingService.Dtos;
using Barakah.ClothingService.Entities;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.ClothingService.Controllers;

[ApiController]
[Route("api/sustainability-ratings")]
public class SustainabilityRatingsController(ClothingDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSustainabilityRatingRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);
        if (request.Score is < 0 or > 100) return BadRequest(new { error = "Score must be between 0 and 100." });

        var rating = new SustainabilityRating
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId!.Value,
            ProductId = request.ProductId,
            Score = request.Score,
            Certifications = request.Certifications,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.SustainabilityRatings.Add(rating);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetByProduct), new { productId = rating.ProductId }, ToResponse(rating));
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var rating = await db.SustainabilityRatings.FirstOrDefaultAsync(r => r.ProductId == productId, cancellationToken);
        return rating is null ? NotFound() : Ok(ToResponse(rating));
    }

    [HttpPut("product/{productId:guid}")]
    public async Task<IActionResult> Update(Guid productId, UpdateSustainabilityRatingRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);
        if (request.Score is < 0 or > 100) return BadRequest(new { error = "Score must be between 0 and 100." });

        var rating = await db.SustainabilityRatings.FirstOrDefaultAsync(r => r.ProductId == productId, cancellationToken);
        if (rating is null) return NotFound();

        rating.Score = request.Score;
        rating.Certifications = request.Certifications;
        rating.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(rating));
    }

    private static SustainabilityRatingResponse ToResponse(SustainabilityRating r) => new(
        r.Id, r.TenantId, r.ProductId, r.Score, r.Certifications, r.CreatedAt, r.UpdatedAt);
}
