using Barakah.IdentityService.Entities;
using Barakah.IdentityService.Services;
using Xunit;

namespace Barakah.IdentityService.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(new JwtOptions
    {
        Secret = "test-secret-at-least-32-characters-long!!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30,
    });

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "irrelevant",
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void GenerateAccessToken_ProducesNonEmptyToken()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(CreateUser(), ["Cashier"]);

        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesDistinctRawTokenAndHash()
    {
        var service = CreateService();
        var (rawToken, tokenHash, expiresAt) = service.GenerateRefreshToken();

        Assert.NotEqual(rawToken, tokenHash);
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var service = CreateService();
        var (rawToken, _, _) = service.GenerateRefreshToken();

        var hash1 = service.HashRefreshToken(rawToken);
        var hash2 = service.HashRefreshToken(rawToken);

        Assert.Equal(hash1, hash2);
    }
}
