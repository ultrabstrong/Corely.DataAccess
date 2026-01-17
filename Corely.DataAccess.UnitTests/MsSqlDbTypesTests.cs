using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public class MsSqlDbTypesTests : DbTypesTestsBase
{
    protected override IDbTypes DbTypes => new MsSqlDbTypes();

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsMsSql()
    {
        Assert.Equal(DatabaseType.MsSql, DbTypes.ConfiguredForDatabaseType);
    }
}
