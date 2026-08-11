namespace Barakah.NotificationService.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? OrderId { get; set; }
    public string Channel { get; set; } = default!;
    public string Recipient { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
