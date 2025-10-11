using AutoFixture;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.UnitTests.EntityFramework.Repos;

public class EFReadonlyRepoAdapterTests
{
    private static ServiceProvider BuildProvider(bool registerSecondContext = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<DbContextFixture>(o =>
            o.UseInMemoryDatabase(new Fixture().Create<string>())
        );
        if (registerSecondContext)
            services.AddDbContext<AnotherDbContextFixture>(o =>
                o.UseInMemoryDatabase(new Fixture().Create<string>())
            );

        services.RegisterEntityFrameworkReposAndUoW();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ReadonlyAdapter_Get_List_Count_Any_Work()
    {
        using var provider = BuildProvider();

        // Seed through EF Core directly
        var ctx = provider.GetRequiredService<DbContextFixture>();
        ctx.Set<EntityFixture>()
            .AddRange(
                new EntityFixture { Id = 10 },
                new EntityFixture { Id = 11 },
                new EntityFixture { Id = 12 }
            );
        await ctx.SaveChangesAsync();

        var repo = provider.GetRequiredService<IReadonlyRepo<EntityFixture>>();

        var any = await repo.AnyAsync(e => e.Id == 11);
        Assert.True(any);

        var countAll = await repo.CountAsync();
        Assert.Equal(3, countAll);

        var countFiltered = await repo.CountAsync(e => e.Id >= 11);
        Assert.Equal(2, countFiltered);

        var single = await repo.GetAsync(e => e.Id == 12);
        Assert.NotNull(single);
        Assert.Equal(12, single!.Id);

        var listAsc = await repo.ListAsync(orderBy: q => q.OrderBy(e => e.Id));
        Assert.Equal([10, 11, 12], [.. listAsc.Select(e => e.Id)]);

        var listDesc = await repo.ListAsync(orderBy: q => q.OrderByDescending(e => e.Id));
        Assert.Equal([12, 11, 10], [.. listDesc.Select(e => e.Id)]);
    }

    [Fact]
    public void AutoMap_FindsRegisteredContextForEntity()
    {
        using var provider = BuildProvider();
        var map = provider.GetRequiredService<IEFContextResolver>();
        var ctxType = map.GetContextTypeFor(typeof(EntityFixture));
        Assert.Equal(typeof(DbContextFixture), ctxType);
    }

    [Fact]
    public void AutoMap_ThrowsForAmbiguousContext()
    {
        using var provider = BuildProvider(registerSecondContext: true);
        var map = provider.GetRequiredService<IEFContextResolver>();
        Assert.Throws<InvalidOperationException>(() =>
            map.GetContextTypeFor(typeof(EntityFixture))
        );
    }
}
