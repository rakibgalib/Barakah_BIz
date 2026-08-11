namespace Barakah.TenantService.Dtos;

public record CreateTenantRequest(string Name, string Subdomain, string Email, string Tier);

public record UpdateTenantStatusRequest(string Status);

public record TenantResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string Email,
    string Tier,
    string Status,
    DateTime CreatedAt);
