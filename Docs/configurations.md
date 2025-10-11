# Configurations

> For concrete usage examples (registration, provider swapping), open the `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` projects. They show real registrations for InMemory, MySql, and Postgres plus repository + UoW integration.

Configurations abstract EF Core provider setup and unify DB-specific type metadata for audit columns.

## IEFConfiguration
```csharp
public interface IEFConfiguration
{
    void Configure(DbContextOptionsBuilder optionsBuilder);
    IEFDbTypes GetDbTypes();
}
```
- Configure is called by your DbContext.OnConfiguring when options aren’t already configured.
- GetDbTypes provides a small set of provider-specific type hints used by entity configuration helpers (CreatedUtc/ModifiedUtc).

## Provided Base Classes
| Base | Use Case | Notes |
|------|----------|-------|
| EFInMemoryConfigurationBase | Tests / demos | Provider ignores column types; still supplies placeholders |
| EFMySqlConfigurationBase | MySQL / MariaDB | UTC defaults via UTC_TIMESTAMP |
| EFPostgresConfigurationBase | PostgreSQL | UTC defaults via CURRENT_TIMESTAMP |
| EFSqliteConfigurationBase | SQLite | TEXT + CURRENT_TIMESTAMP defaults; see demo for shared in-memory connection pattern |

### Example
```csharp
internal sealed class MySqlDemoConfiguration : EFMySqlConfigurationBase
{
    public MySqlDemoConfiguration(string cs) : base(cs) {}
    public override void Configure(DbContextOptionsBuilder b)
        => b.UseMySql(connectionString, new MySqlServerVersion(new Version(8,0,36)));
}
```

### Swapping Providers
Replace the single IEFConfiguration registration:
```csharp
services.AddSingleton<IEFConfiguration>(new SqliteDemoConfiguration());
```

### Patterns & Tips
- Register one singleton IEFConfiguration that your DbContexts can consume.
- Keep secrets out of code; pass connection strings via configuration.
- For SQLite in-memory demos across multiple contexts, keep a single shared connection alive or use Cache=Shared.
