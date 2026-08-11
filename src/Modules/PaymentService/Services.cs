using System.Net.Http.Json;

namespace Barakah.PaymentService.Services;

/// <summary>
/// Thin typed-HttpClient wrapper around Order Service, same pattern as Order Service's own
/// CatalogClient/InventoryClient. Used to push the resulting payment status back onto the order
/// after a payment is recorded.
/// </summary>
public class OrderClient(HttpClient httpClient)
{
    private record UpdatePaymentStatusRequest(string PaymentStatus);

    public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, string tenantSubdomain, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/orders/{orderId}/payment-status")
        {
            Content = JsonContent.Create(new UpdatePaymentStatusRequest(paymentStatus)),
        };
        request.Headers.Add("X-Tenant-Subdomain", tenantSubdomain);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
