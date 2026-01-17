using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public class InMemoryDbTypesTests : DbTypesTestsBase
{
    protected override IDbTypes DbTypes => new InMemoryDbTypes();

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsInMemory()
    {
        Assert.Equal(DatabaseType.InMemory, DbTypes.ConfiguredForDatabaseType);
    }
}
