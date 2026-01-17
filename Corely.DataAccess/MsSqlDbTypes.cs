namespace Corely.DataAccess;

public class MsSqlDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.MsSql;
    public virtual string UTCDateColumnType => "DATETIME2";
    public virtual string UTCDateColumnDefaultValue => "(SYSUTCDATETIME())";
    public virtual string UuidColumnType => "UNIQUEIDENTIFIER";
    public virtual string UuidColumnDefaultValue => "(NEWID())";
    public virtual string JsonColumnType => "NVARCHAR(MAX)";
    public virtual string BoolColumnType => "BIT";
    public virtual string DecimalColumnType => "DECIMAL(19,4)";
    public virtual string DecimalColumnDefaultValue => "0";
    public virtual string BigIntColumnType => "BIGINT";
}
