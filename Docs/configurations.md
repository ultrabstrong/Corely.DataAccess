# Configurations

> For concrete usage examples (registration, provider swapping), open the `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` projects. They show real registrations for InMemory, MySql, and Postgres plus repository + UoW integration.

Configurations abstract EF Core provider setup and unify DB-specific type metadata for auditing columns.

## IEFConfiguration
```csharp
public interface IEFConfiguration
{
    void Configure(DbContextOptionsBuilder optionsBuilder);
    IEFDbTypes GetDbTypes();
}
```
Implementations provide both EF Core options configuration and database type info (UTC timestamp type & default value) used by entity configuration helpers.

## Provided Base Classes
| Base | Use Case | Notes |
|------|----------|-------|
| EFInMemoryConfigurationBase | Tests / demos | Column types are dummies (in-memory provider ignores them) |
| EFMySqlConfigurationBase | MySQL / MariaDB | Uses TIMESTAMP + UTC_TIMESTAMP default |
| EFPostgresConfigurationBase | PostgreSQL | Uses TIMESTAMP + CURRENT_TIMESTAMP |

### Creating a Provider Configuration
```csharp
internal sealed class MySqlDemoConfiguration : EFMySqlConfigurationBase
{
    public MySqlDemoConfiguration(string cs) : base(cs) {}
    public override void Configure(DbContextOptionsBuilder b)
        => b.UseMySql(connectionString, new MySqlServerVersion(new Version(8,0,36)));
}
```

### Swapping Providers
To change database providers, replace the single `IEFConfiguration` registration:
```csharp
services.AddSingleton<IEFConfiguration>(new MySqlDemoConfiguration(mySqlConnectionString));
```

### Accessing Db Types
EntityConfigurationBase and its helpers consume `IEFDbTypes` from `GetDbTypes()`—application code rarely needs it directly.

### Patterns
- Register one singleton IEFConfiguration per context.
- Keep connection strings outside code (user-secrets, env vars, vault) in real apps.
- Wrap partial differences (logging, retry policies) in a derived configuration.
