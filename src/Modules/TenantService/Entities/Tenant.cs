namespace Barakah.TenantService.Entities;

public static class TenantStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
}

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Subdomain { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Tier { get; set; } = "basic";
    public string Status { get; set; } = TenantStatus.Pending;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionStart { get; set; }
    public DateTime? SubscriptionEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
