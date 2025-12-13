namespace Corely.DataAccess;

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
