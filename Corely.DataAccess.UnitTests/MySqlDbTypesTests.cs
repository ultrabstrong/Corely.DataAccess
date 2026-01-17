using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public class MySqlDbTypesTests : DbTypesTestsBase
{
    protected override IDbTypes DbTypes => new MySqlDbTypes();

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsMySql()
    {
        Assert.Equal(DatabaseType.MySql, DbTypes.ConfiguredForDatabaseType);
    }
}
