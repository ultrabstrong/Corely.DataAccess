namespace Corely.DataAccess;

public class PostgreSqlDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.PostgreSql;
    public virtual string UTCDateColumnType => "TIMESTAMP";
    public virtual string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";
    public virtual string UuidColumnType => "UUID";
    public virtual string UuidColumnDefaultValue => "gen_random_uuid()";
    public virtual string JsonColumnType => "JSONB";
    public virtual string BoolColumnType => "BOOLEAN";
    public virtual string DecimalColumnType => "NUMERIC(19,4)";
    public virtual string DecimalColumnDefaultValue => "0";
    public virtual string BigIntColumnType => "BIGINT";
}
