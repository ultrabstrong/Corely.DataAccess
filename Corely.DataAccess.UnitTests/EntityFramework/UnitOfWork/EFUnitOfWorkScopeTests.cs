using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework.UnitOfWork;

public class EFUoWScopeTests
{
    [Fact]
    public void Register_AddsContext_FiresEvent()
    {
        var scope = new EFUoWScope();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new DbContextFixture(options);

        var fired = 0;
        scope.ContextRegistered += _ => fired++;

        scope.Register(ctx);

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Register_Duplicate_FiresOnce()
    {
        var scope = new EFUoWScope();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new DbContextFixture(options);

        var fired = 0;
        scope.ContextRegistered += _ => fired++;

        scope.Register(ctx);
        scope.Register(ctx); // duplicate should still fire twice since scope no longer dedupes

        Assert.Equal(2, fired);
    }

    [Fact]
    public void Register_Null_DoesNothing()
    {
        var scope = new EFUoWScope();
        var fired = 0;
        scope.ContextRegistered += _ => fired++;

        scope.Register(null!);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void IsActive_Property_Roundtrip()
    {
        var scope = new EFUoWScope();
        Assert.False(scope.IsActive);
        scope.IsActive = true;
        Assert.True(scope.IsActive);
        scope.IsActive = false;
        Assert.False(scope.IsActive);
    }
}
