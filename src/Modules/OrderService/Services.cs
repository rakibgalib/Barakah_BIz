using System.Net.Http.Json;
using Barakah.OrderService.Dtos;

namespace Barakah.OrderService.Services;

/// <summary>
/// Thin typed-HttpClient wrapper around Catalog Service, same pattern as
/// Barakah.TenantContext.HttpTenantResolver. Order Service fetches the current price here
/// rather than trusting a client-supplied price on the order request.
/// </summary>
public class CatalogClient(HttpClient httpClient)
{
    public async Task<ProductLookupResponse?> GetProductAsync(Guid productId, string tenantSubdomain, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/{productId}");
        request.Headers.Add("X-Tenant-Subdomain", tenantSubdomain);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<ProductLookupResponse>(cancellationToken);
    }
}

/// <summary>
/// Thin typed-HttpClient wrapper around Inventory Service. Order creation uses this to look up
/// the stock record for a product/branch and then deduct it via the existing /adjust endpoint,
/// which already refuses to go negative — that refusal is Order's real stock check.
/// </summary>
public class InventoryClient(HttpClient httpClient)
{
    public async Task<InventoryLookupResponse?> GetInventoryAsync(Guid productId, Guid branchId, string tenantSubdomain, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/inventory/{productId}/{branchId}");
        request.Headers.Add("X-Tenant-Subdomain", tenantSubdomain);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<InventoryLookupResponse>(cancellationToken);
    }

    public async Task<bool> AdjustAsync(Guid inventoryItemId, int delta, string tenantSubdomain, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/inventory/{inventoryItemId}/adjust")
        {
            Content = JsonContent.Create(new AdjustInventoryRequest(delta, null)),
        };
        request.Headers.Add("X-Tenant-Subdomain", tenantSubdomain);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
