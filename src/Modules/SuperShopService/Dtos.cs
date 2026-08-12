namespace Barakah.SuperShopService.Dtos;

public record CreateSupplierRequest(string Name, string? ContactEmail, string? ContactPhone);
public record UpdateSupplierRatingRequest(decimal Rating);
public record SupplierResponse(Guid Id, Guid TenantId, string Name, string? ContactEmail, string? ContactPhone, decimal? Rating, bool IsActive, DateTime CreatedAt);

public record EarnPointsRequest(string CustomerId, int Points);
public record RedeemPointsRequest(int Points);
public record LoyaltyAccountResponse(Guid Id, Guid TenantId, string CustomerId, int PointsBalance, int TotalPointsEarned, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateProductBatchRequest(Guid ProductId, string BatchNumber, int Quantity, DateTime ExpiresAt);
public record ProductBatchResponse(Guid Id, Guid TenantId, Guid ProductId, string BatchNumber, int Quantity, DateTime ExpiresAt, DateTime CreatedAt);

public record BulkDiscountRequest(decimal UnitPrice, int Quantity);
public record BulkDiscountResponse(decimal UnitPrice, int Quantity, bool DiscountApplied, decimal DiscountPercentage, decimal TotalPrice);
