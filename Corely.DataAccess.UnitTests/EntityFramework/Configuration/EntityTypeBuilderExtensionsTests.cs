using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Corely.DataAccess.UnitTests.EntityFramework.Configuration;

public class EntityTypeBuilderExtensionsTests
{
    private sealed class DummyDbTypes : IDbTypes
    {
        public string ConfiguredForDatabaseType => "Dummy";
        public string UTCDateColumnType => "datetime";
        public string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";
        public string UuidColumnType => "TEXT";
        public string UuidColumnDefaultValue => "''";
        public string JsonColumnType => "TEXT";
        public string BoolColumnType => "INTEGER";
        public string DecimalColumnType => "TEXT";
        public string DecimalColumnDefaultValue => "'0'";
        public string BigIntColumnType => "INTEGER";
    }

    private sealed class PersonEntity : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
    {
        public int Id { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? ModifiedUtc { get; set; }
        public string? Name { get; set; }
    }

    private sealed class News : IHasIdPk<int>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
    }

    private sealed class PersonEntityConfiguration(IDbTypes db)
        : EntityConfigurationBase<PersonEntity, int>(db)
    {
        protected override void ConfigureInternal(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PersonEntity> b
        )
        {
            b.Property(e => e.Name).HasMaxLength(128);
        }
    }

    private sealed class NewsConfiguration(IDbTypes db) : EntityConfigurationBase<News, int>(db)
    {
        protected override void ConfigureInternal(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<News> b
        )
        {
            b.Property(e => e.Title).HasMaxLength(200);
        }
    }

    private sealed class InMemoryTestContext : DbContext
    {
        public DbSet<PersonEntity> People => Set<PersonEntity>();
        public DbSet<News> News => Set<News>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var db = new DummyDbTypes();
            modelBuilder.ApplyConfiguration(new PersonEntityConfiguration(db));
            modelBuilder.ApplyConfiguration(new NewsConfiguration(db));
        }
    }

    private sealed class SqliteTestContext : DbContext
    {
        public DbSet<PersonEntity> People => Set<PersonEntity>();
        public DbSet<News> News => Set<News>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite("Data Source=:memory:");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var db = new DummyDbTypes();
            modelBuilder.ApplyConfiguration(new PersonEntityConfiguration(db));
            modelBuilder.ApplyConfiguration(new NewsConfiguration(db));
        }
    }

    [Fact]
    public void ConfigureTable_StripsEntitySuffixAndPluralizes()
    {
        using var ctx = new InMemoryTestContext();
        var entityType = ctx.Model.FindEntityType(typeof(PersonEntity));
        Assert.NotNull(entityType);
        Assert.Equal("Persons", entityType!.GetTableName());
    }

    [Fact]
    public void ConfigureTable_DoesNotAddS_WhenAlreadyEndsWithS()
    {
        using var ctx = new InMemoryTestContext();
        var entityType = ctx.Model.FindEntityType(typeof(News));
        Assert.NotNull(entityType);
        Assert.Equal("News", entityType!.GetTableName());
    }

    [Fact]
    public void ConfigureIdPk_SetsKeyAndValueGeneratedOnAdd()
    {
        using var ctx = new InMemoryTestContext();
        var entityType = ctx.Model.FindEntityType(typeof(PersonEntity));
        Assert.NotNull(entityType);

        var pk = entityType!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal("Id", pk.Properties[0].Name);

        var idProp = entityType.FindProperty("Id");
        Assert.NotNull(idProp);
        Assert.Equal(ValueGenerated.OnAdd, idProp!.ValueGenerated);
    }

    [Fact]
    public void ConfigureCreatedUtc_SetsBehaviors_And_RelationalMapping()
    {
        // Verify save behaviors and value generation using provider-agnostic model
        using (var ctx = new InMemoryTestContext())
        {
            var entityType = ctx.Model.FindEntityType(typeof(PersonEntity));
            Assert.NotNull(entityType);
            var createdProp = entityType!.FindProperty(nameof(IHasCreatedUtc.CreatedUtc));
            Assert.NotNull(createdProp);
            Assert.Equal(ValueGenerated.OnAdd, createdProp!.ValueGenerated);
            Assert.Equal(PropertySaveBehavior.Ignore, createdProp.GetBeforeSaveBehavior());
            Assert.Equal(PropertySaveBehavior.Ignore, createdProp.GetAfterSaveBehavior());
        }

        // Verify relational-specific mapping using a relational provider (SQLite)
        using (var ctx = new SqliteTestContext())
        {
            var entityType = ctx.Model.FindEntityType(typeof(PersonEntity));
            Assert.NotNull(entityType);
            var createdProp = entityType!.FindProperty(nameof(IHasCreatedUtc.CreatedUtc));
            Assert.NotNull(createdProp);
            Assert.Equal("datetime", createdProp!.GetColumnType());
            Assert.Equal("CURRENT_TIMESTAMP", createdProp.GetDefaultValueSql());
        }
    }

    [Fact]
    public void ConfigureModifiedUtc_SetsColumnType()
    {
        using var ctx = new SqliteTestContext();
        var entityType = ctx.Model.FindEntityType(typeof(PersonEntity));
        Assert.NotNull(entityType);
        var modifiedProp = entityType!.FindProperty(nameof(IHasModifiedUtc.ModifiedUtc));
        Assert.NotNull(modifiedProp);
        Assert.Equal("datetime", modifiedProp!.GetColumnType());
    }
}
