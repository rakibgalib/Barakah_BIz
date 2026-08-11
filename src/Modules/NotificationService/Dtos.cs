namespace Barakah.NotificationService.Dtos;

public record CreateNotificationRequest(Guid? OrderId, string Channel, string Recipient, string Message);

public record NotificationResponse(
    Guid Id,
    Guid TenantId,
    Guid? OrderId,
    string Channel,
    string Recipient,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt);
