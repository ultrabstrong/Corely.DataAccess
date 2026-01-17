# Configurations

> For concrete usage examples (registration, provider swapping), open the `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` projects. They show real registrations for InMemory, MySql, and Postgres plus repository + UoW integration.

Configurations abstract EF Core provider setup and unify DB-specific type metadata for entity columns.

## IEFConfiguration
```csharp
public interface IEFConfiguration
{
    void Configure(DbContextOptionsBuilder optionsBuilder);
    IDbTypes GetDbTypes();
}
```
- Configure is called by your DbContext.OnConfiguring when options aren't already configured.
- GetDbTypes provides a set of provider-specific type hints used by entity configuration helpers.

## IDbTypes
```csharp
public interface IDbTypes
{
    string ConfiguredForDatabaseType { get; }
    string UTCDateColumnType { get; }
    string UTCDateColumnDefaultValue { get; }
    string UuidColumnType { get; }
    string UuidColumnDefaultValue { get; }
    string JsonColumnType { get; }
    string BoolColumnType { get; }
    string DecimalColumnType { get; }
    string DecimalColumnDefaultValue { get; }
    string BigIntColumnType { get; }
}
```

### Provider Type Mappings

| Property | MySQL | PostgreSQL | SQLite | MS SQL |
|----------|-------|------------|--------|--------|
| ConfiguredForDatabaseType | `MySql` | `PostgreSql` | `Sqlite` | `MsSql` |
| UTCDateColumnType | `TIMESTAMP` | `TIMESTAMP` | `TEXT` | `DATETIME2` |
| UTCDateColumnDefaultValue | `(UTC_TIMESTAMP)` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `(SYSUTCDATETIME())` |
| UuidColumnType | `CHAR(36)` | `UUID` | `TEXT` | `UNIQUEIDENTIFIER` |
| UuidColumnDefaultValue | `(UUID())` | `gen_random_uuid()` | *(randomblob expression)* | `(NEWID())` |
| JsonColumnType | `JSON` | `JSONB` | `TEXT` | `NVARCHAR(MAX)` |
| BoolColumnType | `TINYINT(1)` | `BOOLEAN` | `INTEGER` | `BIT` |
| DecimalColumnType | `DECIMAL(19,4)` | `NUMERIC(19,4)` | `TEXT` | `DECIMAL(19,4)` |
| DecimalColumnDefaultValue | `0` | `0` | `'0'` | `0` |
| BigIntColumnType | `BIGINT` | `BIGINT` | `INTEGER` | `BIGINT` |

### Standalone DbTypes Classes
The following classes implement `IDbTypes` and are available in the `Corely.DataAccess` namespace:
- `MySqlDbTypes`
- `PostgreSqlDbTypes`
- `SqliteDbTypes`
- `MsSqlDbTypes`
- `InMemoryDbTypes`

## Provided Base Classes
| Base | Use Case | Notes |
|------|----------|-------|
| EFInMemoryConfigurationBase | Tests / demos | Provider ignores column types; still supplies placeholders |
| EFMySqlConfigurationBase | MySQL / MariaDB | UTC defaults via UTC_TIMESTAMP |
| EFPostgresConfigurationBase | PostgreSQL | UTC defaults via CURRENT_TIMESTAMP |
| EFSqliteConfigurationBase | SQLite | TEXT + CURRENT_TIMESTAMP defaults; see below for in-memory sharing |
| EFMsSqlConfigurationBase | MS SQL Server | UTC defaults via SYSUTCDATETIME() |

### Example
```csharp
internal sealed class MySqlDemoConfiguration : EFMySqlConfigurationBase
{
    public MySqlDemoConfiguration(string cs) : base(cs) {}
    public override void Configure(DbContextOptionsBuilder b)
        => b.UseMySql(connectionString, new MySqlServerVersion(new Version(8,0,36)));
}
```

### Customizing DB Types
The `GetDbTypes()` method is virtual on all base classes, allowing you to override it with custom type mappings:

```csharp
internal sealed class CustomMySqlConfiguration : EFMySqlConfigurationBase
{
    private readonly CustomDbTypes _customTypes = new();

    public CustomMySqlConfiguration(string cs) : base(cs) {}

    public override void Configure(DbContextOptionsBuilder b)
   => b.UseMySql(connectionString, new MySqlServerVersion(new Version(8,0,36)));

    public override IDbTypes GetDbTypes() => _customTypes;

    private class CustomDbTypes : IDbTypes
    {
        public string ConfiguredForDatabaseType => DatabaseType.MySql;
        public string UTCDateColumnType => "DATETIME";
        public string UTCDateColumnDefaultValue => "(NOW())";
        public string UuidColumnType => "BINARY(16)";
        public string UuidColumnDefaultValue => "(UUID_TO_BIN(UUID()))";
        public string JsonColumnType => "JSON";
        public string BoolColumnType => "TINYINT(1)";
        public string DecimalColumnType => "DECIMAL(18,2)";
        public string DecimalColumnDefaultValue => "0.00";
        public string BigIntColumnType => "BIGINT";
    }
}
```

Alternatively, inherit from a provider-specific DbTypes class and override only the properties you need:

```csharp
public class CustomMySqlDbTypes : MySqlDbTypes
{
    public override string DecimalColumnType => "DECIMAL(18,2)";
    public override string DecimalColumnDefaultValue => "0.00";
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
