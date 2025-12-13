using Corely.DataAccess.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<TEntity> ConfigureTable<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    )
        where TEntity : class
    {
        var tableName = typeof(TEntity).Name;
        if (tableName.EndsWith("Entity"))
        {
            tableName = tableName.Replace("Entity", string.Empty);
        }
        if (!tableName.EndsWith('s'))
        {
            tableName += "s";
        }
        builder.ToTable(tableName);
        return builder;
    }

    public static EntityTypeBuilder<TEntity> ConfigureIdPk<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder
    )
        where TEntity : class, IHasIdPk<TKey>
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        return builder;
    }

    public static EntityTypeBuilder<TEntity> ConfigureCreatedUtc<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IDbTypes dbTypes
    )
        where TEntity : class
    {
        if (typeof(IHasCreatedUtc).IsAssignableFrom(typeof(TEntity)))
        {
            var prop = builder
                .Property(e => ((IHasCreatedUtc)e).CreatedUtc)
                .HasColumnType(dbTypes.UTCDateColumnType)
                .HasDefaultValueSql(dbTypes.UTCDateColumnDefaultValue)
                .ValueGeneratedOnAdd()
                .IsRequired();

            // Ensure EF doesn't try to write CreatedUtc on insert/update; let the DB set it once
            prop.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            prop.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }

        return builder;
    }

    public static EntityTypeBuilder<TEntity> ConfigureModifiedUtc<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IDbTypes dbTypes
    )
        where TEntity : class
    {
        if (typeof(IHasModifiedUtc).IsAssignableFrom(typeof(TEntity)))
        {
            builder
                .Property(e => ((IHasModifiedUtc)e).ModifiedUtc)
                .HasColumnType(dbTypes.UTCDateColumnType);
        }

        return builder;
    }
}
