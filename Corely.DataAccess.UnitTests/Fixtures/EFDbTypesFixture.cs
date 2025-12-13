using Corely.DataAccess.EntityFramework.Configurations;

namespace Corely.DataAccess.UnitTests.Fixtures;

internal class EFDbTypesFixture : IEFDbTypes
{
    public string UTCDateColumnType => nameof(UTCDateColumnType);

    public string UTCDateColumnDefaultValue => nameof(UTCDateColumnDefaultValue);

    public string UuidColumnType => nameof(UuidColumnType);

    public string UuidColumnDefaultValue => nameof(UuidColumnDefaultValue);

    public string JsonColumnType => nameof(JsonColumnType);

    public string BoolColumnType => nameof(BoolColumnType);

    public string DecimalColumnType => nameof(DecimalColumnType);

    public string DecimalColumnDefaultValue => nameof(DecimalColumnDefaultValue);

    public string BigIntColumnType => nameof(BigIntColumnType);
}
