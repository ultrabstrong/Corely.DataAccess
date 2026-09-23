namespace Corely.DataAccess;

public class SqliteDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.Sqlite;

    public virtual string UTCDateColumnType => "TEXT";
    public virtual string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";

    public virtual string UuidColumnType => "TEXT";
    public virtual string UuidColumnDefaultValue =>
        "(lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))))";

    public virtual string JsonColumnType => "TEXT";

    public virtual string BoolColumnType => "INTEGER";

    public virtual string DecimalColumnType => "TEXT";
    public virtual string DecimalColumnDefaultValue => "'0'";

    public virtual string BigIntColumnType => "INTEGER";
}
