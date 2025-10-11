using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework.UnitOfWork;

public class EFUnitOfWorkScopeTests
{
    [Fact]
    public void Register_AddsContext_AndAppearsInContexts()
    {
        var scope = new EFUnitOfWorkScope();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new DbContextFixture(options);

        scope.Register(ctx);

        Assert.Contains(ctx, scope.Contexts);
        Assert.Single(scope.Contexts);
    }

    [Fact]
    public void Register_Duplicate_DoesNotAddTwice_AndEventFiresOnce()
    {
        var scope = new EFUnitOfWorkScope();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new DbContextFixture(options);

        var eventCount = 0;
        scope.ContextRegistered += _ => eventCount++;

        scope.Register(ctx);
        scope.Register(ctx); // duplicate

        Assert.Contains(ctx, scope.Contexts);
        Assert.Single(scope.Contexts);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Register_Null_DoesNothing()
    {
        var scope = new EFUnitOfWorkScope();
        var eventCount = 0;
        scope.ContextRegistered += _ => eventCount++;

        scope.Register(null!);

        Assert.Empty(scope.Contexts);
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void ContextRegistered_Fires_ForEachNewContext()
    {
        var scope = new EFUnitOfWorkScope();
        var db1 = Guid.NewGuid().ToString();
        var db2 = Guid.NewGuid().ToString();

        using var ctx1 = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(db1).Options
        );
        using var ctx2 = new AnotherDbContextFixture(
            new DbContextOptionsBuilder<AnotherDbContextFixture>().UseInMemoryDatabase(db2).Options
        );

        var eventCount = 0;
        scope.ContextRegistered += _ => eventCount++;

        scope.Register(ctx1);
        scope.Register(ctx2);

        Assert.Equal(2, eventCount);
        Assert.Equal(2, scope.Contexts.Count);
        Assert.Contains(ctx1, scope.Contexts);
        Assert.Contains(ctx2, scope.Contexts);
    }

    [Fact]
    public void IsActive_Property_Roundtrip()
    {
        var scope = new EFUnitOfWorkScope();
        Assert.False(scope.IsActive);
        scope.IsActive = true;
        Assert.True(scope.IsActive);
        scope.IsActive = false;
        Assert.False(scope.IsActive);
    }
}
