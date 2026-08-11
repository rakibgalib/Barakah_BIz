using Barakah.IdentityService.Services;
using Xunit;

namespace Barakah.IdentityService.Tests;

public class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_Succeeds()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        Assert.True(_hasher.Verify("correct-horse-battery-staple", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_DoesNotReturnPlaintext()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        Assert.NotEqual("correct-horse-battery-staple", hash);
    }
}
