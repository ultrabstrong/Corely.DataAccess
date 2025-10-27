# Step-by-Step Setup

This guide shows how to get started using Corely.DataAccess.

## 1) Install NuGet Packages
Core library (always):
```bash
dotnet add package Corely.DataAccess
```
Logger (needed for service registration)
```bash
# Simplest logger to start with is Microsoft console logger
dotnet add package Microsoft.Extensions.Logging.Console
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
using Microsoft.Data.Sqlite;

internal sealed class SqliteAppConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection? _sqliteConnection;

    public SqliteAppConfiguration(string connectionString) : base(connectionString)
    {
        // Keep the connection open for in-memory databases so the DB remains alive
        var csb = new SqliteConnectionStringBuilder(connectionString);
        var isInMemory = string.Equals(csb.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
                         || csb.Mode == SqliteOpenMode.Memory;
        if (isInMemory)
        {
            _sqliteConnection = new SqliteConnection(connectionString);
            _sqliteConnection.Open();
        }

    }
    public override void Configure(DbContextOptionsBuilder b)
    {
        if (_sqliteConnection == null)
            b.UseSqlite(connectionString);
        else
            b.UseSqlite(_sqliteConnection);
    }
}
```
In-memory example:
```csharp
internal sealed class InMemoryAppConfiguration(string dbName) : EFInMemoryConfigurationBase
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

## 4) Entity Configuration
To enable audit helpers and conventions, derive from `EntityConfigurationBase<>`:
```csharp
using Corely.DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TodoItemConfiguration(IEFDbTypes db) : EntityConfigurationBase<TodoItem, int>(db)
{
    // optional override to customize configuration
    protected override void ConfigureInternal(EntityTypeBuilder<TodoItem> b)
        => b.Property(e => e.Title).IsRequired().HasMaxLength(200);
}
```

## 5) Create Your DbContext
Two options:

A) Simplest (DbContextBase)
```csharp
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

internal sealed class AppDbContext : DbContextBase
{
    public AppDbContext(IEFConfiguration efConfiguration) : base(efConfiguration) { }
    public AppDbContext(DbContextOptions<AppDbContext> opts, IEFConfiguration efConfiguration)
        : base(efConfiguration) { }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    // Apply entity configurations
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
}
```
See [DbContextBase](dbcontext-base.md) for details.

B) Provider-agnostic with explicit OnModelCreating
```csharp
internal sealed class AppDbContext : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public AppDbContext(IEFConfiguration efConfiguration) 
        : base() { _efConfiguration = efConfiguration; }

    public AppDbContext(DbContextOptions<AppDbContext> opts, IEFConfiguration efConfiguration) 
        : base(opts) { _efConfiguration = efConfiguration; }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _efConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicit configs may be preferred for assemblies with multiple contexts
        var configTypes = new Type[] { typeof(EntityConfigurationBase<>), typeof(EntityConfigurationBase<,>) };
        foreach (var configType in configTypes)
        {
            var configs = GetType()
                .Assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true }
                    && t.BaseType.GetGenericTypeDefinition() == configType
                );

            foreach (var t in configs)
            {
                var cfg = Activator.CreateInstance(t, _efConfiguration.GetDbTypes());
                modelBuilder.ApplyConfiguration((dynamic)cfg!);
            }
        }
    }
}
```

## 6) Register Services (DI)
Use the helper to wire repositories. Also register a provider configuration and your DbContext(s).
```csharp
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole()); // Can replace with your preferred logger
// Use a named shared in-memory SQLite DB for demos/tests
services.AddSingleton<IEFConfiguration>(new SqliteAppConfiguration("Data Source=docstodata;Mode=Memory;Cache=Shared"));
services.AddDbContext<AppDbContext>(); // internal types are fine within your project

// Repos + UoW (standard path)
services.RegisterEntityFrameworkReposAndUoW();
```

Note (tests/demos only): Ensure the schema exists for your DbContext before running queries. For ephemeral providers (InMemory, SQLite in-memory) or simple demos, you can call EnsureCreated to create tables:
```csharp
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
ctx.Database.EnsureCreated();
```
Avoid EnsureCreated in production. Prefer migrations (dotnet ef database update) or your organization’s deployment process to create/update schemas.

For provider‑free unit tests, you can instead do:
```csharp
services.RegisterMockReposAndUoW();
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
    var repo = sp.GetRequiredService<IRepo<TodoItem>>();
    await repo.CreateAsync(new TodoItem { Id = 2, Title = "Batch" });
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
