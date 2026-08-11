namespace Barakah.IdentityService.Dtos;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? Phone);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, UserResponse User);

public record UserResponse(Guid Id, string Email, string FirstName, string LastName, Guid? TenantId, bool EmailVerified);
