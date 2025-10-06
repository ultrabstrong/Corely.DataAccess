# Corely.DataAccess

High-level abstractions for data access that keep the domain/persistence boundary clean. The library provides:

- Provider-agnostic Entity Framework configuration abstraction (IEFConfiguration)
- Database provider base configurations (InMemory, MySql, Postgres)
- Entity configuration helpers (EntityConfigurationBase + extension methods for common columns)
- Generic repositories (readonly + full CRUD)
- Lightweight unit of work (transaction) abstraction
- Demo project showing registration and usage
- Mock repositories for fast unit tests

## Quick Start
```bash
dotnet add package Corely.DataAccess
```

Minimal (single DbContext, no custom subclasses):
```csharp
services.AddSingleton<IEFConfiguration>(new InMemoryConfiguration());
services.AddScoped<MyDbContext>();
services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepo<>));
services.AddScoped(typeof(IRepo<>), typeof(EFRepo<>));
services.AddScoped<IUnitOfWorkProvider, EFUoWProvider>(); // optional but recommended for batching
```
Full / Custom (context-specific repo & UoW subclasses):
```csharp
services.AddSingleton<IEFConfiguration>(new InMemoryConfiguration());
services.AddScoped<MyDbContext>();
// Custom repo + UoW subclasses
services.AddScoped(typeof(IReadonlyRepo<>), typeof(MyReadonlyRepo<>));
services.AddScoped(typeof(IRepo<>), typeof(MyRepo<>));
services.AddScoped<IUnitOfWorkProvider, MyUoWProvider>();
```

## Key Concepts
| Topic | Summary |
|-------|---------|
| IEFConfiguration | Abstraction to configure EF Core provider + expose unified db type metadata. |
| Provider Bases | EFInMemoryConfigurationBase, EFMySqlConfigurationBase, EFPostgresConfigurationBase inherit IEFConfiguration. |
| Entity Configuration | EntityConfigurationBase classes centralize common auditing + id setup. |
| Repositories | EFReadonlyRepo / EFRepo provide generic query + CRUD patterns with extension points. |
| Unit of Work | EFUoWProvider coordinates optional transactions. |
| Mock Repositories | In-memory implementations for fast tests without provider dependencies. |

## Documentation
- [Configurations](configurations.md)
- [Entity Configuration & Property Helpers](entity-configuration.md)
- [Repositories](repositories.md)
- [Unit of Work](unit-of-work.md)
- [Mock Repositories](mock-repositories.md)

## Demo
See Corely.DataAccess.Demo and Corely.DataAccess.DemoApp for:
- Switching providers (uncomment MySql/Postgres)
- Automatic entity configuration discovery
- Open generic repo registration (or custom wrappers)

## Philosophy
Keep domain model free of persistence concerns while still leveraging EF Core efficiently. Favor composition and small interfaces; provide defaults but allow overrides via subclassing or replacing services.
