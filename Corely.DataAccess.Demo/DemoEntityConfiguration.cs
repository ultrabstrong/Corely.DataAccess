using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.DataAccess.Demo;

internal class DemoEntityConfiguration(IEFDbTypes efDbTypes)
    : EntityConfigurationBase<DemoEntity, int>(efDbTypes)
{
    protected override void ConfigureInternal(EntityTypeBuilder<DemoEntity> builder)
    {
        // Additional configuration specific to DemoEntity
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
    }
}
