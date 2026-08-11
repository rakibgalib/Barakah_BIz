namespace Barakah.CatalogService.Dtos;

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal BasePrice,
    string BusinessType,
    string? Category,
    string? Attributes);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string? Category,
    string? Attributes);

public record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal BasePrice,
    string BusinessType,
    string? Category,
    string Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
