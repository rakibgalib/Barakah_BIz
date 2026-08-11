using Barakah.TenantService.Data;
using Barakah.TenantService.Dtos;
using Barakah.TenantService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Barakah.TenantService.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController(TenantDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (await db.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain, cancellationToken))
        {
            return Conflict(new { error = "A tenant with this subdomain already exists." });
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Subdomain = request.Subdomain,
            Email = request.Email,
            Tier = request.Tier,
            Status = TenantStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        // Provision the per-tenant schema (products/inventory/orders/order_items/employees)
        // via the create_tenant_schema() function defined in scripts/init-db.sql.
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT create_tenant_schema(@tenantId)";
            command.Parameters.AddWithValue("tenantId", tenant.Id.ToString("N"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, ToResponse(tenant));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FindAsync([id], cancellationToken);
        return tenant is null ? NotFound() : Ok(ToResponse(tenant));
    }

    [HttpGet("by-subdomain/{subdomain}")]
    public async Task<ActionResult<TenantResponse>> GetBySubdomain(string subdomain, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);
        return tenant is null ? NotFound() : Ok(ToResponse(tenant));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TenantResponse>> UpdateStatus(Guid id, UpdateTenantStatusRequest request, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FindAsync([id], cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        tenant.Status = request.Status;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(tenant));
    }

    private static TenantResponse ToResponse(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Subdomain, tenant.Email, tenant.Tier, tenant.Status, tenant.CreatedAt);
}
