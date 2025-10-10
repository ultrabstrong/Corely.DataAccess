using AutoFixture;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.UnitTests.EntityFramework.Repos;

public class EFRepoAdapterTests
{
    private static ServiceProvider BuildProvider(bool registerSecondContext = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register DbContexts
        services.AddDbContext<DbContextFixture>(o => o.UseInMemoryDatabase(new Fixture().Create<string>()));
        if (registerSecondContext)
            services.AddDbContext<AnotherDbContextFixture>(o => o.UseInMemoryDatabase(new Fixture().Create<string>()));

        // Wire Corely repos + adapters + map
        services.AutoRegisterEntityFrameworkProviders();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RepoAdapter_CanCRUD_ViaResolvedContext()
    {
        using var provider = BuildProvider();
        var repo = provider.GetRequiredService<IRepo<EntityFixture>>();

        var e = new EntityFixture { Id = 42 };
        await repo.CreateAsync(e);
        var fetched = await repo.GetAsync(x => x.Id == 42);
        Assert.NotNull(fetched);

        await repo.UpdateAsync(new EntityFixture { Id = 42 });
        var list = await repo.ListAsync();
        Assert.Single(list);

        await repo.DeleteAsync(list[0]);
        Assert.False(await repo.AnyAsync(x => true));
    }

    [Fact]
    public async Task ReadonlyRepoAdapter_CanQuery_ViaResolvedContext()
    {
        using var provider = BuildProvider();
        var repo = provider.GetRequiredService<IReadonlyRepo<EntityFixture>>();

        // Seed through EF Core directly
        var ctx = provider.GetRequiredService<DbContextFixture>();
        ctx.Set<EntityFixture>().AddRange(new EntityFixture { Id = 1 }, new EntityFixture { Id = 2 });
        await ctx.SaveChangesAsync();

        var list = await repo.ListAsync();
        Assert.Equal(2, list.Count);
        Assert.True(await repo.AnyAsync(x => x.Id == 1));
        Assert.Equal(2, await repo.CountAsync());
    }

    [Fact]
    public void AutoMap_FindsRegisteredContextForEntity()
    {
        using var provider = BuildProvider();
        var map = provider.GetRequiredService<IEntityContextMap>();
        var ctxType = map.GetContextTypeFor(typeof(EntityFixture));
        Assert.Equal(typeof(DbContextFixture), ctxType);
    }

    [Fact]
    public void AutoMap_ThrowsForAmbiguousContext()
    {
        using var provider = BuildProvider(registerSecondContext: true);
        var map = provider.GetRequiredService<IEntityContextMap>();
        Assert.Throws<InvalidOperationException>(() => map.GetContextTypeFor(typeof(EntityFixture)));
    }
}
