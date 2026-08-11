namespace Barakah.PaymentService.Dtos;

public record CreatePaymentRequest(Guid OrderId, decimal Amount, string Method, string? TransactionReference);

public record PaymentResponse(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Method,
    string Status,
    string? TransactionReference,
    DateTime CreatedAt);
