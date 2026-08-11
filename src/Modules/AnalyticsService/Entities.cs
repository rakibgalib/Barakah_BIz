namespace Barakah.AnalyticsService.Entities;

/// <summary>
/// Read-only projection of OrderService's "orders" table.
/// Analytics never writes here — see AnalyticsReadDbContext.
/// </summary>
public class OrderRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public string PaymentStatus { get; set; } = "pending";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Read-only projection of PaymentService's "payments" table.
/// Analytics never writes here — see AnalyticsReadDbContext.
/// </summary>
public class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}
