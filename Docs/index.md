# Corely.DataAccess

High-level abstractions over EF Core that keep the domain/persistence boundary clean while staying practical.

What you get:
- Decoupled persistence layer: domain works against small repo + UoW interfaces; EF Core is the default implementation but not required. You can plug in other providers (e.g., Dapper) by implementing the same interfaces.
- Provider-agnostic EF configuration abstraction (IEFConfiguration + base classes)
- Entity configuration helpers (EntityConfigurationBase + column helpers)
- Generic repositories (readonly + CRUD) with adapters that auto-map to your DbContexts
- Lightweight Unit of Work provider (deferred SaveChanges + transactions when supported)
- Mock repositories and UoW for fast unit tests
- Demo projects showing end-to-end registration and usage

## Quick Start
```bash
dotnet add package Corely.DataAccess
```

## Registration
Choose a provider configuration (see [Configurations](configurations.md)) and register one or more DbContexts. Adapters automatically resolve the correct DbContext for each entity at runtime.
```csharp
services.AddSingleton<IEFConfiguration>(new SqliteDemoConfiguration()); // or MySql/Postgres/InMemory
services.AddDbContext<MyDbContext>(); 
// Can register multiple DbContexts if needed

// Repos + UoW (standard path)
services.RegisterEntityFrameworkReposAndUoW();

// Alternatively, mocks can be registered via services.RegisterMockReposAndUoW() for fast tests.
```
Note: To use a non‑EF provider (e.g., Dapper), implement IReadonlyRepo<T>, IRepo<T>, and IUnitOfWorkProvider yourself and register those services in DI instead of the EF adapters/UoW.

## Usage
```csharp
// For EF, if none or multiple contexts match to this entity, an error is thrown.
var repo = sp.GetRequiredService<IRepo<MyEntity>>();
await repo.CreateAsync(new MyEntity { /*...*/ });

var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
await repo.UpdateAsync(entity);
await uow.CommitAsync();
```

## Key behaviors
- For EF, repositories and the unit of work automatically resolve and use the appropriate DbContext for each entity at runtime.
- While a unit of work is active, repository changes are deferred and saved together; on relational providers they are committed atomically.
- Mock repositories/UoW can be swapped in for fast, provider‑free unit tests.

## Documentation
- [Step-by-Step Setup](step-by-step-setup.md)
- [Configurations](configurations.md)
- [DbContextBase](dbcontext-base.md)
- [Entity Configuration & Property Helpers](entity-configuration.md)
- [Repositories](repositories.md)
- [Unit of Work](unit-of-work.md)
- [Mock Repositories](mock-repositories.md)

## Demo
See Corely.DataAccess.Demo and Corely.DataAccess.DemoApp for:
- Switching providers (SQLite in-memory demo included)
- Automatic entity configuration discovery
- Adapter registration and UoW usage

## Philosophy
Keep domain code free of persistence details. Prefer small, composable abstractions; provide sensible defaults with easy escape hatches.
