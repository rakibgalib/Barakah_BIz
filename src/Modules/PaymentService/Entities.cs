namespace Barakah.PaymentService.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public string? TransactionReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
