using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class EntityConfigurationBaseTests
{
    private class TestEntityConfiguration : EntityConfigurationBase<EntityFixture>
    {
        public bool IsDbTypesSet => DbTypes != null;
        public bool ConfigureInternalCalled { get; private set; }

        public TestEntityConfiguration(IDbTypes dbTypes)
            : base(dbTypes) { }

        protected override void ConfigureInternal(EntityTypeBuilder<EntityFixture> builder)
        {
            ConfigureInternalCalled = true;
        }
    }

    private class TestEntityConfigurationWithKey : EntityConfigurationBase<EntityFixture, int>
    {
        public bool IsDbTypesSet => DbTypes != null;
        public bool ConfigureInternalCalled { get; private set; }

        public TestEntityConfigurationWithKey(IDbTypes dbTypes)
            : base(dbTypes) { }

        protected override void ConfigureInternal(EntityTypeBuilder<EntityFixture> builder)
        {
            ConfigureInternalCalled = true;
        }
    }

    private readonly EFDbTypesFixture _dbTypes = new();

    [Fact]
    public void Configure_CallsConfigureInternal()
    {
        var modelBuilder = new ModelBuilder();
        var entityBuilder = modelBuilder.Entity<EntityFixture>();
        var entityConfiguration = new TestEntityConfiguration(_dbTypes);

        entityConfiguration.Configure(entityBuilder);

        Assert.True(entityConfiguration.IsDbTypesSet);
        Assert.True(entityConfiguration.ConfigureInternalCalled);

        var entityType = modelBuilder.Model.FindEntityType(typeof(EntityFixture));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType.GetTableName());
        Assert.Null(entityType.FindProperty(nameof(EntityFixture.Id)));
        Assert.NotNull(entityType.FindProperty(nameof(EntityFixture.CreatedUtc)));
        Assert.NotNull(entityType.FindProperty(nameof(EntityFixture.ModifiedUtc)));
    }

    [Fact]
    public void Configure_CallsConfigureInternalWithKey()
    {
        var modelBuilder = new ModelBuilder();
        var entityBuilder = modelBuilder.Entity<EntityFixture>();
        var entityConfiguration = new TestEntityConfigurationWithKey(_dbTypes);

        entityConfiguration.Configure(entityBuilder);

        Assert.True(entityConfiguration.IsDbTypesSet);
        Assert.True(entityConfiguration.ConfigureInternalCalled);

        var entityType = modelBuilder.Model.FindEntityType(typeof(EntityFixture));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(EntityFixture.Id)));
        Assert.NotNull(entityType.FindProperty(nameof(EntityFixture.CreatedUtc)));
        Assert.NotNull(entityType.FindProperty(nameof(EntityFixture.ModifiedUtc)));
    }
}
