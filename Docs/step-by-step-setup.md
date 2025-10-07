# Step-by-Step Setup (Minimal -> Custom)

This guide walks you from an empty project to a working data access setup using Corely.DataAccess.

## 1. Install NuGet Packages
Core library (always):
```bash
dotnet add package Corely.DataAccess
```
EF Core provider packages (choose what you need):
```bash
# In-Memory (tests / demos)
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# MySQL (example uses Pomelo)
dotnet add package Pomelo.EntityFrameworkCore.MySql

# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

## 2. Pick / Create an IEFConfiguration
You need a class implementing `IEFConfiguration` (inherit from the appropriate base):
- `EFInMemoryConfigurationBase`
- `EFMySqlConfigurationBase`
- `EFPostgresConfigurationBase`

Minimal In-Memory example (adapted from demo):
```csharp
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

public sealed class InMemoryAppConfiguration(string dbName) : EFInMemoryConfigurationBase
{
    private readonly string _dbName = dbName;
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase(_dbName);
}
```
(MySql/Postgres versions just call `UseMySql(...)` / `UseNpgsql(...)` respectively inside `Configure`).

## 3. Create an Entity
Create your POCO(s). Optionally implement the timestamp / id interfaces to leverage built-in helpers.
```csharp
using Corely.DataAccess.Interfaces.Entities; // optional interfaces

public class TodoItem : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
```
Interfaces are optional – they just enable automatic configuration or timestamp behavior.

## 4. (Optional) Entity Configuration
If you want auditing helpers (`CreatedUtc`, `ModifiedUtc`) or automatic table naming, create a configuration (supply `IEFDbTypes` in ctor):
```csharp
using Corely.DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TodoItemConfiguration(IEFDbTypes dbTypes) : EntityConfigurationBase<TodoItem, int>(dbTypes)
{
    protected override void ConfigureInternal(EntityTypeBuilder<TodoItem> builder)
    {
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
    }
}
```
If you skip this, EF will infer a basic model – you just won’t get the provided audit column helpers.

## 5. Create Your DbContext
Provider-agnostic: only depends on `IEFConfiguration` and dynamically discovers any `EntityConfigurationBase<>` descendants.
```csharp
using Corely.DataAccess.EntityFramework; // EntityConfigurationBase<> 
using Corely.DataAccess.EntityFramework.Configurations; // IEFConfiguration
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public AppDbContext(IEFConfiguration efConfiguration)
    {
        _efConfiguration = efConfiguration;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IEFConfiguration efConfiguration) : base(options)
    {
        _efConfiguration = efConfiguration;
    }

    // DbSets
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            _efConfiguration.Configure(optionsBuilder);
        }
    }

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
```

## 6. Register Minimal Services (DI)
```csharp
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework; // EFUoWProvider
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;

var services = new ServiceCollection();
services.AddSingleton<IEFConfiguration>(new InMemoryAppConfiguration("app-db"));
services.AddScoped<AppDbContext>();
services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepo<>) );
services.AddScoped(typeof(IRepo<>), typeof(EFRepo<>));
services.AddScoped<IUnitOfWorkProvider, EFUoWProvider>(); // Optional but recommended
var provider = services.BuildServiceProvider();
```

## 7. Use a Repository
```csharp
var repo = provider.GetRequiredService<IRepo<TodoItem>>();
if (!await repo.AnyAsync(t => t.Id > 0))
{
    await repo.CreateAsync(new TodoItem { Id = 1, Title = "First" });
}
var all = await repo.ListAsync();
```

## 8. (Optional) Unit of Work Scope
```csharp
var uow = provider.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
await repo.CreateAsync(new TodoItem { Id = 2, Title = "Batch" }); // not saved yet
await uow.CommitAsync(); // persists
```

## 9. Switching Providers Later
Only swap the `IEFConfiguration` registration:
```csharp
// MySQL
services.AddSingleton<IEFConfiguration>(new MySqlAppConfiguration(myConnectionString));
// PostgreSQL
services.AddSingleton<IEFConfiguration>(new PostgresAppConfiguration(pgConnection));
```
Your `DbContext`, entities, and repositories remain unchanged.

## 10. Going Further (Customization)
Subclass repos for context-specific behavior:
```csharp
public sealed class AppReadonlyRepo<TEntity>(ILogger<EFReadonlyRepo<TEntity>> logger, AppDbContext ctx)
    : EFReadonlyRepo<TEntity>(logger, ctx) where TEntity : class { }

public sealed class AppRepo<TEntity>(ILogger<EFRepo<TEntity>> logger, AppDbContext ctx)
    : EFRepo<TEntity>(logger, ctx) where TEntity : class { }
```
Then register those instead of the generic base types.

## Summary
You now have:
1. Provider abstraction via `IEFConfiguration`
2. A provider-agnostic `DbContext`
3. Optional entity configurations with auditing helpers
4. Generic repositories + optional Unit of Work
5. Easy provider switching (swap one registration)

Move to the other docs for deeper topics:
- configurations.md
- entity-configuration.md
- repositories.md
- unit-of-work.md
- mock-repositories.md
