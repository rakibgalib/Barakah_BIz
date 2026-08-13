namespace Barakah.ClothingService.Dtos;

public record CreateSustainabilityRatingRequest(Guid ProductId, int Score, string? Certifications);
public record UpdateSustainabilityRatingRequest(int Score, string? Certifications);

public record SustainabilityRatingResponse(
    Guid Id, Guid TenantId, Guid ProductId, int Score, string? Certifications, DateTime CreatedAt, DateTime UpdatedAt);
