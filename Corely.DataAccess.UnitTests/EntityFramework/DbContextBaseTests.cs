using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class DbContextBaseTests
{
    private sealed class TestDbContext : DbContextBase
    {
        public TestDbContext(IEFConfiguration ef)
            : base(ef) { }

        public TestDbContext(DbContextOptions<TestDbContext> opts, IEFConfiguration ef)
            : base(opts, ef) { }

        public DbSet<EntityFixture> Entities => Set<EntityFixture>();
    }

    [Fact]
    public void OnConfiguring_UsesProvidedIEFConfiguration()
    {
        var efConfig = new EFConfigurationFixture();
        var options = new DbContextOptionsBuilder<TestDbContext>().Options;
        using var ctx = new TestDbContext(options, efConfig);
        // Accessing Database.ProviderName forces configuration to be applied
        Assert.NotNull(ctx.Database.ProviderName);
    }

    [Fact]
    public void OnModelCreating_AppliesEntityConfigurations()
    {
        var efConfig = new EFConfigurationFixture();
        using var ctx = new TestDbContext(efConfig);
        // Ensure model built and entity type exists
        var entityType = ctx.Model.FindEntityType(typeof(EntityFixture));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType!.FindProperty(nameof(EntityFixture.CreatedUtc)));
    }
}
