using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.Demo;

internal class DemoEntityConfiguration(IDbTypes dbTypes)
    : EntityConfigurationBase<DemoEntity, int>(dbTypes)
{
    protected override void ConfigureInternal(EntityTypeBuilder<DemoEntity> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
    }
}

internal class DemoEntity2Configuration(IDbTypes dbTypes)
    : EntityConfigurationBase<DemoEntity2, int>(dbTypes) { }
