using Corely.DataAccess;

namespace Corely.DataAccess.UnitTests;

public abstract class DbTypesTestsBase
{
    protected abstract IDbTypes DbTypes { get; }

    [Fact]
    public void ConfiguredForDatabaseType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.ConfiguredForDatabaseType);
    }

    [Fact]
    public void UTCDateColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.UTCDateColumnType);
    }

    [Fact]
    public void UTCDateColumnDefaultValue_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.UTCDateColumnDefaultValue);
    }

    [Fact]
    public void UuidColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.UuidColumnType);
    }

    [Fact]
    public void UuidColumnDefaultValue_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.UuidColumnDefaultValue);
    }

    [Fact]
    public void JsonColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.JsonColumnType);
    }

    [Fact]
    public void BoolColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.BoolColumnType);
    }

    [Fact]
    public void DecimalColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.DecimalColumnType);
    }

    [Fact]
    public void DecimalColumnDefaultValue_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.DecimalColumnDefaultValue);
    }

    [Fact]
    public void BigIntColumnType_ReturnsNonEmptyString()
    {
        Assert.NotEmpty(DbTypes.BigIntColumnType);
    }
}
