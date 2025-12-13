namespace Corely.DataAccess;

public class MySqlDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.MySql;
    public virtual string UTCDateColumnType => "TIMESTAMP";
    public virtual string UTCDateColumnDefaultValue => "(UTC_TIMESTAMP)";
    public virtual string UuidColumnType => "CHAR(36)";
    public virtual string UuidColumnDefaultValue => "(UUID())";
    public virtual string JsonColumnType => "JSON";
    public virtual string BoolColumnType => "TINYINT(1)";
    public virtual string DecimalColumnType => "DECIMAL(19,4)";
    public virtual string DecimalColumnDefaultValue => "0";
    public virtual string BigIntColumnType => "BIGINT";
}
