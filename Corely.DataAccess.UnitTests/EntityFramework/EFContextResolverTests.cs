using AutoFixture;
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class EFContextResolverTests
{
    // Test-only entities and DbContexts
    private class ResolverEntity1
    {
        public int Id { get; set; }
    }

    private class ResolverEntity2
    {
        public int Id { get; set; }
    }

    private class ResolverEntity3
    {
        public int Id { get; set; }
    }

    private class ResolverDbContext1 : DbContext
    {
        public ResolverDbContext1(DbContextOptions<ResolverDbContext1> options)
            : base(options) { }

        public DbSet<ResolverEntity1> Entities => Set<ResolverEntity1>();
    }

    private class ResolverDbContext2 : DbContext
    {
        public ResolverDbContext2(DbContextOptions<ResolverDbContext2> options)
            : base(options) { }

        public DbSet<ResolverEntity1> Entities => Set<ResolverEntity1>(); // same entity as DbContext1 (for ambiguity)
    }

    private class ResolverDbContext3 : DbContext
    {
        public ResolverDbContext3(DbContextOptions<ResolverDbContext3> options)
            : base(options) { }

        public DbSet<ResolverEntity3> Entities => Set<ResolverEntity3>();
    }

    private static void ClearResolverCache()
    {
        var field = typeof(EFContextResolver).GetField(
            "_cache",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        var dict = field?.GetValue(null);
        var clear = dict?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        clear?.Invoke(dict, null);
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure(services);
        services.RegisterEntityFrameworkReposAndUoW();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void GetContextTypeFor_SingleMatch_ReturnsRegisteredContext()
    {
        ClearResolverCache();
        using var provider = BuildProvider(s =>
        {
            s.AddDbContext<ResolverDbContext1>(o =>
                o.UseInMemoryDatabase(new Fixture().Create<string>())
            );
        });
        var resolver = provider.GetRequiredService<IEFContextResolver>();

        var ctxType = resolver.GetContextTypeFor(typeof(ResolverEntity1));
        Assert.Equal(typeof(ResolverDbContext1), ctxType);
    }

    [Fact]
    public void GetContextTypeFor_NoMatch_Throws()
    {
        ClearResolverCache();
        using var provider = BuildProvider(_ => { });
        var resolver = provider.GetRequiredService<IEFContextResolver>();

        Assert.Throws<InvalidOperationException>(() =>
            resolver.GetContextTypeFor(typeof(ResolverEntity2))
        );
    }

    [Fact]
    public void GetContextTypeFor_Ambiguous_Throws()
    {
        ClearResolverCache();
        using var provider = BuildProvider(s =>
        {
            var dbName = new Fixture().Create<string>();
            s.AddDbContext<ResolverDbContext1>(o => o.UseInMemoryDatabase(dbName));
            s.AddDbContext<ResolverDbContext2>(o => o.UseInMemoryDatabase(dbName));
        });
        var resolver = provider.GetRequiredService<IEFContextResolver>();

        Assert.Throws<InvalidOperationException>(() =>
            resolver.GetContextTypeFor(typeof(ResolverEntity1))
        );
    }

    [Fact]
    public void GetContextTypeFor_CachesResult_ForSameEntity()
    {
        ClearResolverCache();
        using var provider = BuildProvider(s =>
        {
            s.AddDbContext<ResolverDbContext3>(o =>
                o.UseInMemoryDatabase(new Fixture().Create<string>())
            );
        });
        var resolver = provider.GetRequiredService<IEFContextResolver>();

        var t1 = resolver.GetContextTypeFor(typeof(ResolverEntity3));
        var t2 = resolver.GetContextTypeFor(typeof(ResolverEntity3));
        Assert.Same(t1, t2);
        Assert.Equal(typeof(ResolverDbContext3), t1);
    }
}
