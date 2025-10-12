using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Corely.DataAccess.EntityFramework;

public abstract class DbContextBase : DbContext
{
    protected readonly IEFConfiguration EfConfiguration;

    protected DbContextBase(IEFConfiguration efConfiguration)
    {
        EfConfiguration = efConfiguration;
    }

    protected DbContextBase(DbContextOptions options, IEFConfiguration efConfiguration)
        : base(options)
    {
        EfConfiguration = efConfiguration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            EfConfiguration.Configure(optionsBuilder);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discover configurations from provided assemblies (defaults to this context's assembly)
        var configAssemblies = GetConfigurationAssemblies();
        var configurationType = typeof(EntityConfigurationBase<>);
        foreach (var asm in configAssemblies)
        {
            IEnumerable<Type> types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null)!.Cast<Type>();
            }

            foreach (var t in types)
            {
                if (t is null || !t.IsClass || t.IsAbstract)
                    continue;
                var bt = t.BaseType;
                if (bt is null || !bt.IsGenericType)
                    continue;
                if (bt.GetGenericTypeDefinition() != configurationType)
                    continue;

                var instance = Activator.CreateInstance(t, EfConfiguration.GetDbTypes());
                // Use dynamic to call the correct generic ApplyConfiguration
                modelBuilder.ApplyConfiguration((dynamic)instance!);
            }
        }

        // Allow derived contexts to add custom model configuration
        ConfigureModel(modelBuilder);
    }

    /// <summary>
    /// Override to add or replace assemblies that contain EntityConfigurationBase<> implementations.
    /// Defaults to the assembly that defines the current context type.
    /// </summary>
    protected virtual IEnumerable<Assembly> GetConfigurationAssemblies() => [GetType().Assembly];

    /// <summary>
    /// Override to add context-specific model configuration in addition to discovered configurations.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected virtual void ConfigureModel(ModelBuilder modelBuilder) { }
}
