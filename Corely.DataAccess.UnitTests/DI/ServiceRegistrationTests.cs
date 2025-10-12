using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.DI;

public class ServiceRegistrationTests
{
    private ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<DbContextFixture>(o => o.UseInMemoryDatabase(dbName));
        services.RegisterEntityFrameworkReposAndUoW();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void UoW_InterfaceAndConcrete_AreSameScopedInstance()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var scope = sp.CreateScope();

        var concrete = scope.ServiceProvider.GetRequiredService<EFUoWProvider>();
        var viaInterface = scope.ServiceProvider.GetRequiredService<IUnitOfWorkProvider>();

        Assert.Same(concrete, viaInterface);
    }

    [Fact]
    public void UoW_DifferentScopes_GetDifferentInstances()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var s1 = sp.CreateScope();
        using var s2 = sp.CreateScope();

        var uow1 = s1.ServiceProvider.GetRequiredService<EFUoWProvider>();
        var uow2 = s2.ServiceProvider.GetRequiredService<EFUoWProvider>();

        Assert.NotSame(uow1, uow2);
    }

    [Fact]
    public void Repo_ReceivesSameUoWInstance_AsInterfaceResolution()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var scope = sp.CreateScope();

        var uowConcrete = scope.ServiceProvider.GetRequiredService<EFUoWProvider>();
        var uowIface = scope.ServiceProvider.GetRequiredService<IUnitOfWorkProvider>();
        Assert.Same(uowConcrete, uowIface);

        // Resolve a concrete EFRepo via DI so its constructor injection runs
        var repo = scope.ServiceProvider.GetRequiredService<
            EFRepo<DbContextFixture, EntityFixture>
        >();

        // Reflect the private _uow field to ensure it is the same instance
        var uowField = typeof(EFRepo<DbContextFixture, EntityFixture>).GetField(
            "_uow",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        Assert.NotNull(uowField);
        var repoUow = uowField!.GetValue(repo);
        Assert.Same(uowConcrete, repoUow);
    }

    [Fact]
    public void Adapter_ResolvesConcreteRepo_WithSameUoWInstance()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var scope = sp.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<EFUoWProvider>();

        // Resolve adapter and force it to create the concrete repo inside the same scope
        var adapter = scope.ServiceProvider.GetRequiredService<IRepo<EntityFixture>>();
        // Perform an innocuous call to ensure the inner EFRepo is constructed
        var _ = adapter.AnyAsync(e => e.Id == -1).GetAwaiter().GetResult();

        // Resolve the concrete EFRepo directly and verify it sees the same UoW (constructor-injected)
        var concreteRepo = scope.ServiceProvider.GetRequiredService<
            EFRepo<DbContextFixture, EntityFixture>
        >();
        var uowField = typeof(EFRepo<DbContextFixture, EntityFixture>).GetField(
            "_uow",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        var repoUow = uowField!.GetValue(concreteRepo);
        Assert.Same(uow, repoUow);
    }
}
