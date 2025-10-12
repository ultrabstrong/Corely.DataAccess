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

internal class DemoDbContext : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public DemoDbContext(
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_1_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base() => _efConfiguration = efConfiguration;

    public DemoDbContext(
        DbContextOptions<DemoDbContext> options,
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_1_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(options) => _efConfiguration = efConfiguration;

    public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _efConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DemoEntityConfiguration(_efConfiguration.GetDbTypes()));
    }
}

internal class DemoDbContext2 : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public DemoDbContext2(
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_2_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base() => _efConfiguration = efConfiguration;

    public DemoDbContext2(
        DbContextOptions<DemoDbContext2> options,
        [FromKeyedServices(ContextConfigurationKeys.CONTEXT_2_CONFIG)]
            IEFConfiguration efConfiguration
    )
        : base(options) => _efConfiguration = efConfiguration;

    public DbSet<DemoEntity2> DemoEntities2 => Set<DemoEntity2>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _efConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new DemoEntity2Configuration(_efConfiguration.GetDbTypes())
        );
    }
}
