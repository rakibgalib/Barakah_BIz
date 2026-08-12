namespace Barakah.RestaurantService.Dtos;

public record CreateMenuItemRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string Category,
    string? AllergenTags);

public record UpdateMenuItemRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string Category,
    string? AllergenTags,
    bool IsAvailable);

public record MenuItemResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    decimal BasePrice,
    string Category,
    string? AllergenTags,
    bool IsAvailable,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PricedMenuItemResponse(
    Guid Id,
    string Name,
    decimal BasePrice,
    decimal CurrentPrice,
    string PricingTier);
