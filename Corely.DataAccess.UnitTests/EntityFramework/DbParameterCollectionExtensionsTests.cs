using Corely.DataAccess.EntityFramework;
using Microsoft.Data.Sqlite;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class DbParameterCollectionExtensionsTests
{
    private static SqliteParameterCollection Parameters(params (string Name, object Value)[] ps)
    {
        var command = new SqliteCommand();
        foreach (var (name, value) in ps)
            command.Parameters.Add(new SqliteParameter(name, value));
        return command.Parameters;
    }

    [Fact]
    public void ToLoggingDictionary_KeepsValues_WhenLoggingValues()
    {
        var dict = Parameters(("@id", 7), ("@name", "n")).ToLoggingDictionary(logValues: true);

        Assert.Equal(7, dict["@id"]);
        Assert.Equal("n", dict["@name"]);
    }

    [Fact]
    public void ToLoggingDictionary_HidesValues_WhenNotLoggingValues()
    {
        var dict = Parameters(("@secret", "s")).ToLoggingDictionary(logValues: false);

        Assert.Equal("?", dict["@secret"]);
    }

    [Fact]
    public void ToLoggingDictionary_NamesUnnamedParametersByPosition()
    {
        var dict = Parameters(("@a", 1), ("", 2)).ToLoggingDictionary(logValues: true);

        Assert.Equal(2, dict["p1"]);
    }

    [Fact]
    public void ToLoggingDictionary_IgnoresNameCase()
    {
        var dict = Parameters(("@Id", 1)).ToLoggingDictionary(logValues: true);

        Assert.Equal(1, dict["@id"]);
    }
}
