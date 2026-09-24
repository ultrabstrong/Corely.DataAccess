using Corely.DataAccess.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.Extensions;

public static class EntityTypeBuilderExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        public EntityTypeBuilder<TEntity> ConfigureTable()
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

        public EntityTypeBuilder<TEntity> ConfigureCreatedUtc(IDbTypes dbTypes)
        {
            if (typeof(IHasCreatedUtc).IsAssignableFrom(typeof(TEntity)))
            {
                var prop = builder
                    .Property(e => ((IHasCreatedUtc)e).CreatedUtc)
                    .HasColumnType(dbTypes.UTCDateColumnType)
                    .HasDefaultValueSql(dbTypes.UTCDateColumnDefaultValue)
                    .ValueGeneratedOnAdd()
                    .IsRequired();

                prop.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
                prop.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            }

            return builder;
        }

        public EntityTypeBuilder<TEntity> ConfigureModifiedUtc(IDbTypes dbTypes)
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

    public static EntityTypeBuilder<TEntity> ConfigureIdPk<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder
    )
        where TEntity : class
    {
        if (typeof(IHasGeneratedIdPk<TKey>).IsAssignableFrom(typeof(TEntity)))
        {
            builder.HasKey(e => ((IHasGeneratedIdPk<TKey>)e).Id);
            builder.Property(e => ((IHasGeneratedIdPk<TKey>)e).Id).ValueGeneratedOnAdd();
        }
        return builder;
    }
}
