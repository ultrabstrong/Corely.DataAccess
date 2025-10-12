# Context Configuration (DbContextBase)

DbContextBase is a small base DbContext that removes common boilerplate:
- Wires provider configuration via an IEFConfiguration instance in OnConfiguring
- Discovers and applies entity configurations that derive from EntityConfigurationBase<>
- Exposes two protected hooks so you can customize discovery and add extra model configuration

Use it to keep your DbContexts concise and consistent across projects.

## What it does

1) Provider configuration
- If DbContextOptions weren’t preconfigured, OnConfiguring calls EfConfiguration.Configure(optionsBuilder).
- This defers all provider selection (SQLite, InMemory, MySQL, Postgres) to your IEFConfiguration implementation.

2) Entity configuration discovery
- OnModelCreating scans assemblies returned by GetConfigurationAssemblies() for types that directly derive from EntityConfigurationBase<>.
- Each configuration is constructed with the IEFDbTypes from EfConfiguration.GetDbTypes() and applied to the model.
- After discovery, ConfigureModel(modelBuilder) is invoked for any additional per-context configuration.

Notes:
- EF caches the model per context type; the scan runs during model build, not on every query.
- By default, only the current context’s assembly is scanned.

## Basic usage

```csharp
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContextBase
{
    public AppDbContext(IEFConfiguration ef) : base(ef) {}
    public AppDbContext(DbContextOptions<AppDbContext> opts, IEFConfiguration ef) : base(opts, ef) {}

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
```

That’s it—no need to repeat OnConfiguring/OnModelCreating in each context. Place your EntityConfigurationBase<> classes in the same assembly as the context so they’re discovered automatically.

## Protected hooks

### GetConfigurationAssemblies()
Signature:
```csharp
protected virtual IEnumerable<Assembly> GetConfigurationAssemblies()
```
Default:
```csharp
new[] { GetType().Assembly }
```
When to override:
- Your entity configuration classes live in a different assembly (shared library, feature module).
- You want to aggregate configurations from multiple assemblies.

Example: include another assembly
```csharp
protected override IEnumerable<Assembly> GetConfigurationAssemblies()
{
    yield return GetType().Assembly;              // this context’s assembly
    yield return typeof(SharedEntityConfiguration).Assembly; // another assembly
}
```

### ConfigureModel(ModelBuilder modelBuilder)
Signature:
```csharp
protected virtual void ConfigureModel(ModelBuilder modelBuilder)
```
Default: no-op.
When to override:
- Add per-context conventions, indices, query filters, owned types, or other model adjustments that aren’t in a specific EntityConfigurationBase<>.

Example: global query filter
```csharp
protected override void ConfigureModel(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<TodoItem>()
        .HasQueryFilter(e => !e.IsDeleted);
}
```

## Requirements and conventions
- Discovered configuration classes must derive directly from EntityConfigurationBase<>, and have a constructor that accepts an IEFDbTypes.
- If you don’t use EntityConfigurationBase<>, you can still override ConfigureModel to apply configuration manually.
- IEFConfiguration must be registered in DI and provided to the context; DbContextBase will call Configure only if options aren’t already configured.

## Demo references
- DemoDbContext and DemoDbContext2 in the demo project inherit DbContextBase.
- Their entity configurations (e.g., DemoEntityConfiguration) are applied automatically at startup.

## When to use this base
Use DbContextBase when you want:
- Consistent provider setup across contexts via IEFConfiguration
- Automatic discovery of EntityConfigurationBase<> without repeating reflection logic
- A clean place (ConfigureModel) to put small, context-specific tweaks

If you prefer explicit configuration per context, you can skip the base and copy the short pattern shown above into your own DbContexts; the library works either way.
