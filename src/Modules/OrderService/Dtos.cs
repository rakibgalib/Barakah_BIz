namespace Barakah.OrderService.Dtos;

public record OrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderRequest(Guid BranchId, Guid? CustomerId, List<OrderItemRequest> Items, string? PaymentMethod, Guid? CreatedBy);

public record UpdateOrderStatusRequest(string Status);

public record UpdateOrderPaymentStatusRequest(string PaymentStatus);

public record OrderItemResponse(Guid ProductId, int Quantity, decimal UnitPrice, decimal TotalPrice, decimal Discount);

public record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid BranchId,
    Guid? CustomerId,
    string Status,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    string? PaymentMethod,
    string PaymentStatus,
    DateTime CreatedAt,
    List<OrderItemResponse> Items);

// Contracts for the calls this service makes into Catalog/Inventory.
public record ProductLookupResponse(Guid Id, string Sku, string Name, decimal BasePrice);

public record InventoryLookupResponse(Guid Id, Guid ProductId, Guid BranchId, int Quantity, int MinStockLevel, int MaxStockLevel, DateTime LastUpdated);

public record AdjustInventoryRequest(int Delta, Guid? UpdatedBy);
