# Step-by-Step Setup

This guide shows how to get started using Corely.DataAccess.

## 1) Install NuGet Packages
Core library (always):
```bash
dotnet add package Corely.DataAccess
```
EF Core provider packages (choose what you need):
```bash
# SQLite (demo-friendly)
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# In-Memory (tests / demos)
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# MySQL (example uses Pomelo)
dotnet add package Pomelo.EntityFrameworkCore.MySql

# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

## 2) Pick / Create an IEFConfiguration
Provide a class implementing `IEFConfiguration` (inherit from the appropriate base):
- `EFSqliteConfigurationBase`
- `EFInMemoryConfigurationBase`
- `EFMySqlConfigurationBase`
- `EFPostgresConfigurationBase`

SQLite example:
```csharp
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteAppConfiguration(string connectionString) : EFSqliteConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder b)
        => b.UseSqlite(connectionString);
}
```
In-memory example:
```csharp
public sealed class InMemoryAppConfiguration(string dbName) : EFInMemoryConfigurationBase
{
    public override void Configure(DbContextOptionsBuilder b)
        => b.UseInMemoryDatabase(dbName);
}
```
Learn more in the [Configurations](configurations.md) docs.

## 3) Create an Entity
Optionally implement timestamp/id interfaces for helpers (CreatedUtc/ModifiedUtc, Id key). Entities are typically internal.
```csharp
using Corely.DataAccess.Interfaces.Entities;

internal class TodoItem : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
```
Learn more in the [Entity Configuration](entity-configuration.md) docs.

## 4) (Optional) Entity Configuration
To enable audit helpers and conventions, derive from `EntityConfigurationBase<>`:
```csharp
using Corely.DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TodoItemConfiguration(IEFDbTypes db) : EntityConfigurationBase<TodoItem, int>(db)
{
    protected override void ConfigureInternal(EntityTypeBuilder<TodoItem> b)
        => b.Property(e => e.Title).IsRequired().HasMaxLength(200);
}
```

## 5) Create Your DbContext
Provider-agnostic: depends on `IEFConfiguration` and discovers configurations.

Option A: inherit from DbContextBase (recommended)
```csharp
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

internal sealed class AppDbContext : DbContextBase
{
    public AppDbContext(IEFConfiguration ef) : base(ef) {}
    public AppDbContext(DbContextOptions<AppDbContext> opts, IEFConfiguration ef) : base(opts, ef) {}

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
```
See [Context Configuration](context-configuration.md) for extension hooks.

Option B: implement the pattern directly
```csharp
internal sealed class AppDbContext : DbContext
{
    private readonly IEFConfiguration _ef;
    public AppDbContext(IEFConfiguration ef) { _ef = ef; }
    public AppDbContext(DbContextOptions<AppDbContext> opts, IEFConfiguration ef) : base(opts) { _ef = ef; }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder b)
    {
        if (!b.IsConfigured) _ef.Configure(b);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var cfgType = typeof(EntityConfigurationBase<>);
        var cfgs = GetType().Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == cfgType);
        foreach (var t in cfgs)
        {
            var cfg = Activator.CreateInstance(t, _ef.GetDbTypes());
            mb.ApplyConfiguration((dynamic)cfg!);
        }
    }
}
```

## 6) Register Services (DI)
Use the helper to wire repositories. Also register a provider configuration and your DbContext(s).
```csharp
var services = new ServiceCollection();
services.AddSingleton<IEFConfiguration>(new SqliteAppConfiguration("Data Source=:memory:"));
services.AddDbContext<AppDbContext>(); // internal types are fine within your project

// Repos + UoW (standard path)
services.RegisterEntityFrameworkReposAndUoW();

// For provider‑free unit tests, you can instead do:
// services.RegisterMockReposAndUoW();
```

## 7) Use a Repository
```csharp
var sp = services.BuildServiceProvider();
var repo = sp.GetRequiredService<IRepo<TodoItem>>();
if (!await repo.AnyAsync(t => t.Id > 0))
{
    await repo.CreateAsync(new TodoItem { Id = 1, Title = "First" });
}
var all = await repo.ListAsync();
```
Learn more in the [Repositories](repositories.md) docs.

## 8) Optional: Unit of Work
```csharp
var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
try
{
    var uowRepo = uow.GetRepository<TodoItem>(); // enlists the scoped instance in the current DI scope
    await uowRepo.CreateAsync(new TodoItem { Id = 2, Title = "Batch" });
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```
Learn more in the [Unit of Work](unit-of-work.md) docs.

## 9) Switching Providers Later
Only swap the `IEFConfiguration` registration:
```csharp
// SQLite -> InMemory
services.AddSingleton<IEFConfiguration>(new InMemoryAppConfiguration("app-db"));
```
Your `DbContext`, entities, and repository code remain unchanged.

## 10) Where to next?
- [Configurations](configurations.md)
- [Context Configuration](context-configuration.md)
- [Entity Configuration](entity-configuration.md)
- [Repositories](repositories.md)
- [Unit of Work](unit-of-work.md)
- [Mock Repositories](mock-repositories.md)
