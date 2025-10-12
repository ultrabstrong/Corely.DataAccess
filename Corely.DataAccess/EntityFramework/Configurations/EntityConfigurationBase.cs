using Corely.Common.Extensions;
using Corely.DataAccess.Extensions;
using Corely.DataAccess.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EntityConfigurationBase<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IHasIdPk<TKey>
{
    protected readonly IEFDbTypes EFDbTypes;

    protected EntityConfigurationBase(IEFDbTypes efDbTypes)
    {
        EFDbTypes = efDbTypes.ThrowIfNull(nameof(efDbTypes));
    }

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder = builder
            .ConfigureTable()
            .ConfigureIdPk<TEntity, TKey>()
            .ConfigureCreatedUtc(EFDbTypes)
            .ConfigureModifiedUtc(EFDbTypes);
        ConfigureInternal(builder);
    }

    protected abstract void ConfigureInternal(EntityTypeBuilder<TEntity> builder);
}

public abstract class EntityConfigurationBase<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    protected readonly IEFDbTypes EFDbTypes;

    protected EntityConfigurationBase(IEFDbTypes efDbTypes)
    {
        EFDbTypes = efDbTypes.ThrowIfNull(nameof(efDbTypes));
    }

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder = builder
            .ConfigureTable()
            .ConfigureCreatedUtc(EFDbTypes)
            .ConfigureModifiedUtc(EFDbTypes);

        ConfigureInternal(builder);
    }

    protected abstract void ConfigureInternal(EntityTypeBuilder<TEntity> builder);
}
