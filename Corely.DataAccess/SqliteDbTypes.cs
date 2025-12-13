namespace Corely.DataAccess;

/// <summary>
/// SQLite database types. SQLite has limited native types, so many are stored as TEXT or INTEGER.
/// </summary>
public class SqliteDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.Sqlite;

    // SQLite stores dates as TEXT/NUMERIC; use TEXT with CURRENT_TIMESTAMP default for demo/common cases
    public virtual string UTCDateColumnType => "TEXT";
    public virtual string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";

    // SQLite has no native UUID type; store as TEXT
    public virtual string UuidColumnType => "TEXT";
    public virtual string UuidColumnDefaultValue =>
        "(lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))))";

    // SQLite has no native JSON type; store as TEXT
    public virtual string JsonColumnType => "TEXT";

    // SQLite has no native BOOLEAN type; store as INTEGER (0/1)
    public virtual string BoolColumnType => "INTEGER";

    // SQLite has no native DECIMAL type; store as TEXT for precision or REAL for performance
    public virtual string DecimalColumnType => "TEXT";
    public virtual string DecimalColumnDefaultValue => "'0'";

    // SQLite uses INTEGER for all integer types (dynamically sized)
    public virtual string BigIntColumnType => "INTEGER";
}
