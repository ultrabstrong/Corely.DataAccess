using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo;
public class DemoDbContext : DbContext
{
    // This EF config allows switching between different database providers
    // while keeping the domain layer agnostic of EF and the specific provider.
    private readonly IEFConfiguration _efConfiguration;

    public DemoDbContext(IEFConfiguration efConfiguration)
    {
        _efConfiguration = efConfiguration;
    }

    public DemoDbContext(DbContextOptions<DemoDbContext> options, IEFConfiguration efConfiguration) : base(options)
    {
        _efConfiguration = efConfiguration;
    }

    public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            _efConfiguration.Configure(optionsBuilder);
        }
    }

    // Dynamically apply all entity configurations that inherit from EntityConfigurationBase<T>
    // This keeps the DbContext clean and automatically picks up new configurations.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configurationType = typeof(EntityConfigurationBase<>);
        var configurations = GetType().Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null
                        && t.BaseType.IsGenericType
                        && t.BaseType.GetGenericTypeDefinition() == configurationType);

        foreach (var config in configurations)
        {
            var instance = Activator.CreateInstance(config, _efConfiguration.GetDbTypes());
            modelBuilder.ApplyConfiguration((dynamic)instance!);
        }
    }
}
