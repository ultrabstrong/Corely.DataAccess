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
| EFSqliteConfigurationBase | SQLite | TEXT + CURRENT_TIMESTAMP defaults; see below for in-memory sharing |

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
- For SQLite in-memory demos across multiple contexts, prefer the named in-memory pattern (Mode=Memory;Cache=Shared) or reuse a single open connection.

---

## Logging with EFEventDataLogger (example)

This example shows a provider configuration that wires EF Core diagnostics to `EFEventDataLogger` using `LogTo`, while keeping the SQLite in-memory connection alive.

```csharp
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

internal sealed class SqliteConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection? _sqliteConnection;
    private readonly Microsoft.Extensions.Logging.ILogger _efLogger;

    public SqliteConfiguration(string connectionString, ILoggerFactory loggerFactory)
        : base(connectionString)
    {
        _efLogger = loggerFactory.CreateLogger("EFCore");

        // need to keep the connection open for in-memory dbs
        var isInMemory =
            connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase);
        if (isInMemory)
        {
            _sqliteConnection = new SqliteConnection(connectionString);
            _sqliteConnection.Open();
        }
    }

    public override void Configure(DbContextOptionsBuilder b)
    {
        var builder =
            _sqliteConnection == null
                ? b.UseSqlite(connectionString)
                : b.UseSqlite(_sqliteConnection);

        builder.LogTo(
            logger: (EventData e) =>
                EFEventDataLogger.Write(_efLogger, e, EFEventDataLogger.WriteInfoLogsAs.Debug),
            filter: (eventId, _) => eventId.Id == RelationalEventId.CommandExecuted.Id
        );
#if DEBUG
        builder.EnableSensitiveDataLogging().EnableDetailedErrors();
#endif
    }
}
```

Notes
- The filter keeps logs focused on executed commands. Remove or relax it for broader diagnostics.
- In DEBUG, enabling sensitive data and detailed errors is helpful for development.
- If you prefer an interceptor-based approach, use the `UseCorelyEfLogging` extension provided in `Corely.DataAccess.EntityFramework.Extensions`.
