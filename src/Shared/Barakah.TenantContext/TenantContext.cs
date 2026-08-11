using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace Barakah.TenantContext;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? Subdomain { get; }
    bool HasTenant { get; }
    void SetTenant(Guid tenantId, string subdomain);
}

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? Subdomain { get; private set; }
    public bool HasTenant => TenantId.HasValue;

    public void SetTenant(Guid tenantId, string subdomain)
    {
        TenantId = tenantId;
        Subdomain = subdomain;
    }
}

/// <summary>
/// Resolves the tenant for the request from the "X-Tenant-Subdomain" header (or subdomain
/// once real DNS/host routing is wired up) and pushes it into the scoped ITenantContext so
/// downstream services (e.g. Barakah.Persistence's schema switching) can use it.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string TenantHeaderName = "X-Tenant-Subdomain";

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantResolver resolver)
    {
        var subdomain = context.Request.Headers[TenantHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            var tenantId = await resolver.ResolveTenantIdAsync(subdomain, context.RequestAborted);
            if (tenantId.HasValue)
            {
                tenantContext.SetTenant(tenantId.Value, subdomain);
            }
        }

        await next(context);
    }
}

/// <summary>
/// Implemented per-service (typically backed by the Tenant Service's data or a cache) to turn
/// a subdomain into a tenant id. Kept as an interface here so Barakah.TenantContext has no
/// dependency on EF Core / the tenants table.
/// </summary>
public interface ITenantResolver
{
    Task<Guid?> ResolveTenantIdAsync(string subdomain, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves a subdomain to a tenant id by calling Tenant Service's
/// GET /api/tenants/by-subdomain/{subdomain}. Register with a typed HttpClient whose
/// BaseAddress points at Tenant Service (see appsettings' Services:TenantService).
/// </summary>
public class HttpTenantResolver(System.Net.Http.HttpClient httpClient) : ITenantResolver
{
    private record TenantLookupResponse(Guid Id);

    public async Task<Guid?> ResolveTenantIdAsync(string subdomain, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/tenants/by-subdomain/{Uri.EscapeDataString(subdomain)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tenant = await response.Content.ReadFromJsonAsync<TenantLookupResponse>(cancellationToken);
        return tenant?.Id;
    }
}
