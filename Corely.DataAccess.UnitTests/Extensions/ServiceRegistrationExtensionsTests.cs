using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.Mock;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.UnitTests.Extensions;

public class ServiceRegistrationExtensionsTests
{
    [Fact]
    public void RegisterEntityFrameworkReposAndUoW_ResolvesEFImplementations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<DbContextFixture>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        services.RegisterEntityFrameworkReposAndUoW();
        var provider = services.BuildServiceProvider();

        var readonlyRepo = provider.GetRequiredService<IReadonlyRepo<EntityFixture>>();
        var repo = provider.GetRequiredService<IRepo<EntityFixture>>();
        var uow = provider.GetRequiredService<IUnitOfWorkProvider>();
        var resolver1 = provider.GetRequiredService<IEFContextResolver>();
        var resolver2 = provider.GetRequiredService<IEFContextResolver>();

        Assert.IsType<EFReadonlyRepoAdapter<EntityFixture>>(readonlyRepo);
        Assert.IsType<EFRepoAdapter<EntityFixture>>(repo);
        Assert.IsType<EFUoWProvider>(uow);
        Assert.Same(resolver1, resolver2); // singleton
    }

    [Fact]
    public void RegisterMockReposAndUoW_ResolvesMockImplementations()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterMockReposAndUoW();
        var provider = services.BuildServiceProvider();

        var readonlyRepo = provider.GetRequiredService<IReadonlyRepo<EntityFixture>>();
        var repo = provider.GetRequiredService<IRepo<EntityFixture>>();
        var uow = provider.GetRequiredService<IUnitOfWorkProvider>();

        Assert.IsType<MockReadonlyRepo<EntityFixture>>(readonlyRepo);
        Assert.IsType<MockRepo<EntityFixture>>(repo);
        Assert.IsType<MockUoWProvider>(uow);
    }
}
