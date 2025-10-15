using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.Demo;

internal class DemoEntityConfiguration(IEFDbTypes efDbTypes)
    : EntityConfigurationBase<DemoEntity, int>(efDbTypes)
{
    // You can override ConfigureInternal to add additional configuration
    protected override void ConfigureInternal(EntityTypeBuilder<DemoEntity> builder)
    {
        // Additional configuration specific to DemoEntity
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
    }
}

internal class DemoEntity2Configuration(IEFDbTypes efDbTypes)
    : EntityConfigurationBase<DemoEntity2, int>(efDbTypes) { }
