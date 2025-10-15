using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.Demo;

// This is just to demonstrate multiple DbContexts with different configurations
// Most of the time you will only need one DbContext and one IEFConfiguration
public class ContextConfigurationKeys
{
    public const string CONTEXT_1_CONFIG = nameof(CONTEXT_1_CONFIG);
    public const string CONTEXT_2_CONFIG = nameof(CONTEXT_2_CONFIG);
}

internal class DemoDbContext : DbContextBase
{
    public DemoDbContext(
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_1_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(efConfiguration) { }

    public DemoDbContext(
        DbContextOptions<DbContextBase> options,
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_1_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(options, efConfiguration) { }

    public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DemoEntityConfiguration(efConfiguration.GetDbTypes()));
    }
}

internal class DemoDbContext2 : DbContextBase
{
    public DemoDbContext2(
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_2_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(efConfiguration) { }

    public DemoDbContext2(
        DbContextOptions<DbContextBase> options,
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_2_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(options, efConfiguration) { }

    public DbSet<DemoEntity2> DemoEntities2 => Set<DemoEntity2>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DemoEntity2Configuration(efConfiguration.GetDbTypes()));
    }
}
