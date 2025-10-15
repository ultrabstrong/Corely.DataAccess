using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

internal sealed class SqliteDemoConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection? _sqliteConnection;

    public SqliteDemoConfiguration(string connectionString = "Data Source=:memory:;Cache=Shared")
        : base(connectionString)
    {
        // need to keep the connection open for in-memory dbs
        if (connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            _sqliteConnection = new SqliteConnection(connectionString);
            _sqliteConnection.Open();
        }
    }

    public override void Configure(DbContextOptionsBuilder b)
    {
        if (_sqliteConnection == null)
            b.UseSqlite(connectionString);
        else
            b.UseSqlite(_sqliteConnection);
    }
}
