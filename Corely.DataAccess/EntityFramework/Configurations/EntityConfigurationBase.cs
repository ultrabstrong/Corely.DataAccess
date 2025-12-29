using Corely.Common.Extensions;
using Corely.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EntityConfigurationBase<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    protected readonly IDbTypes DbTypes;

    protected EntityConfigurationBase(IDbTypes dbTypes)
    {
        DbTypes = dbTypes.ThrowIfNull(nameof(dbTypes));
    }

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder = builder
            .ConfigureTable()
            .ConfigureIdPk<TEntity, TKey>()
            .ConfigureCreatedUtc(DbTypes)
            .ConfigureModifiedUtc(DbTypes);
        ConfigureInternal(builder);
    }

    protected virtual void ConfigureInternal(EntityTypeBuilder<TEntity> builder) { }
}

public abstract class EntityConfigurationBase<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    protected readonly IDbTypes DbTypes;

    protected EntityConfigurationBase(IDbTypes dbTypes)
    {
        DbTypes = dbTypes.ThrowIfNull(nameof(dbTypes));
    }

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder = builder
            .ConfigureTable()
            .ConfigureCreatedUtc(DbTypes)
            .ConfigureModifiedUtc(DbTypes);

        ConfigureInternal(builder);
    }

    protected virtual void ConfigureInternal(EntityTypeBuilder<TEntity> builder) { }
}
