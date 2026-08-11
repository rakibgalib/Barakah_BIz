using Barakah.SharedKernel;
using Xunit;

namespace Barakah.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_SetsValueAndIsSuccess()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_SetsErrorAndNotSuccess()
    {
        var result = Result<int>.Failure("something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal("something went wrong", result.Error);
    }
}

public class EntityEqualityTests
{
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) => Id = id;
    }

    [Fact]
    public void Entities_WithSameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var entityA = new TestEntity(id);
        var entityB = new TestEntity(id);

        Assert.Equal(entityA, entityB);
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        var entityA = new TestEntity(Guid.NewGuid());
        var entityB = new TestEntity(Guid.NewGuid());

        Assert.NotEqual(entityA, entityB);
    }
}
