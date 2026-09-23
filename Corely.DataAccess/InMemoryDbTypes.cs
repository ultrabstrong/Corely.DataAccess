namespace Corely.DataAccess;

public class InMemoryDbTypes : IDbTypes
{
    public string ConfiguredForDatabaseType => DatabaseType.InMemory;

    public virtual string UTCDateColumnType => nameof(UTCDateColumnType);
    public virtual string UTCDateColumnDefaultValue => nameof(UTCDateColumnDefaultValue);
    public virtual string UuidColumnType => nameof(UuidColumnType);
    public virtual string UuidColumnDefaultValue => nameof(UuidColumnDefaultValue);
    public virtual string JsonColumnType => nameof(JsonColumnType);
    public virtual string BoolColumnType => nameof(BoolColumnType);
    public virtual string DecimalColumnType => nameof(DecimalColumnType);
    public virtual string DecimalColumnDefaultValue => nameof(DecimalColumnDefaultValue);
    public virtual string BigIntColumnType => nameof(BigIntColumnType);
}
