using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public class SqliteDbTypesTests : DbTypesTestsBase
{
    protected override IDbTypes DbTypes => new SqliteDbTypes();

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsSqlite()
    {
        Assert.Equal(DatabaseType.Sqlite, DbTypes.ConfiguredForDatabaseType);
    }
}
