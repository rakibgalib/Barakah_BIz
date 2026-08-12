using Barakah.Persistence;
using Barakah.SuperShopService.Data;
using Barakah.SuperShopService.Dtos;
using Barakah.SuperShopService.Entities;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.SuperShopService.Controllers;

[ApiController]
[Route("api/loyalty")]
public class LoyaltyController(SuperShopDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetAccount(string customerId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);
        return account is null ? NotFound() : Ok(ToResponse(account));
    }

    [HttpPost("earn")]
    public async Task<IActionResult> Earn(EarnPointsRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);
        if (request.Points <= 0) return BadRequest(new { error = "Points must be positive." });

        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);
        if (account is null)
        {
            account = new LoyaltyAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.TenantId!.Value,
                CustomerId = request.CustomerId,
                PointsBalance = 0,
                TotalPointsEarned = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.LoyaltyAccounts.Add(account);
        }
        account.PointsBalance += request.Points;
        account.TotalPointsEarned += request.Points;
        account.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(account));
    }

    [HttpPost("{customerId}/redeem")]
    public async Task<IActionResult> Redeem(string customerId, RedeemPointsRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);
        if (request.Points <= 0) return BadRequest(new { error = "Points must be positive." });

        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);
        if (account is null) return NotFound();
        if (account.PointsBalance < request.Points) return BadRequest(new { error = "Insufficient points balance." });

        account.PointsBalance -= request.Points;
        account.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(account));
    }

    private static LoyaltyAccountResponse ToResponse(LoyaltyAccount a) => new(
        a.Id, a.TenantId, a.CustomerId, a.PointsBalance, a.TotalPointsEarned, a.CreatedAt, a.UpdatedAt);
}
