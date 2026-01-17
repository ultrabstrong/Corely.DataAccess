using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public class PostgreSqlDbTypesTests : DbTypesTestsBase
{
    protected override IDbTypes DbTypes => new PostgreSqlDbTypes();

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsPostgreSql()
    {
        Assert.Equal(DatabaseType.PostgreSql, DbTypes.ConfiguredForDatabaseType);
    }
}
