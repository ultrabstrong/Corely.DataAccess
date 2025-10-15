using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class DbContextBaseTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContextBase
    {
        public TestDbContext(IEFConfiguration cfg)
            : base(cfg) { }

        public DbSet<TestEntity> Entities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
            base.OnModelCreating(modelBuilder);
        }
    }

    [Fact]
    public void OnConfiguring_Uses_IEFConfiguration()
    {
        // Arrange
        var cfg = new EFConfigurationFixture();
        var ctx = new TestDbContext(cfg);

        // Act
        var providerName = ctx.Database.ProviderName;

        // Assert: Should be configured to EF InMemory by fixture
        Assert.NotNull(providerName);
        Assert.Contains("InMemory", providerName!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Can_Create_And_Query()
    {
        var cfg = new EFConfigurationFixture();
        using var ctx = new TestDbContext(cfg);
        ctx.Database.EnsureCreated();

        ctx.Entities.Add(new TestEntity { Id = 1, Name = "A" });
        ctx.SaveChanges();

        var found = ctx.Entities.Single(e => e.Id == 1);
        Assert.Equal("A", found.Name);
    }
}
