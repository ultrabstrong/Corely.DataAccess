# DbContextBase

A minimal base class to wire your DbContext to an IEFConfiguration without repeating boilerplate.

What it does
- Stores the injected IEFConfiguration
- Calls efConfiguration.Configure(optionsBuilder) from OnConfiguring

When to use
- You want a tiny, provider-agnostic DbContext that defers all provider details to IEFConfiguration
- You can still add custom OnConfiguring logic by overriding and calling base first

Example
```csharp
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

internal sealed class AppDbContext : DbContextBase
{
    // Either constructor works; AddDbContext prefers the one with DbContextOptions<YourContext>
    public AppDbContext(IEFConfiguration efConfiguration) : base(efConfiguration) { }
    public AppDbContext(DbContextOptions<AppDbContext> options, IEFConfiguration efConfiguration)
        : base(efConfiguration) { /* options are provided by DI; configuration is applied in OnConfiguring */ }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    // Apply entity configurations (required for conventions/audit helpers)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configTypes = new[] { typeof(EntityConfigurationBase<>), typeof(EntityConfigurationBase<,>) };
        foreach (var configType in configTypes)
        {
            var configs = GetType().Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.BaseType?.IsGenericType == true
                    && t.BaseType.GetGenericTypeDefinition() == configType);
            foreach (var t in configs)
            {
                var cfg = Activator.CreateInstance(t, efConfiguration.GetDbTypes());
                modelBuilder.ApplyConfiguration((dynamic)cfg!);
            }
        }
    }

    // Optional: add extra provider settings; always call base first
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // e.g., enable detailed errors/logging for local dev
        // optionsBuilder.EnableSensitiveDataLogging();
        // optionsBuilder.EnableDetailedErrors();
    }
}
```

Registration
```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IEFConfiguration>(new SqliteAppConfiguration("Data Source=:memory:"));
services.AddDbContext<AppDbContext>();

// Repos + UoW
services.RegisterEntityFrameworkReposAndUoW();
```

Notes
- For tests/demos with ephemeral providers, call ctx.Database.EnsureCreated() before querying.
- In production, prefer migrations or your deployment process to create/update schema.
- If you need model configuration, add your own OnModelCreating override (as above) or use the configuration base types provided elsewhere in the library.
