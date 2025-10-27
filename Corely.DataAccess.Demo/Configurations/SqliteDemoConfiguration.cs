using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

internal sealed class SqliteDemoConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection? _sqliteConnection;

    // Use a named in-memory database with shared cache so multiple connections/contexts can share it
    public SqliteDemoConfiguration(
        string connectionString = "Data Source=docstodata;Mode=Memory;Cache=Shared"
    )
        : base(connectionString)
    {
        // Keep the connection open for in-memory databases so the DB remains alive for the process lifetime
        var csb = new SqliteConnectionStringBuilder(connectionString);
        var isInMemory =
            string.Equals(csb.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || csb.Mode == SqliteOpenMode.Memory;
        if (isInMemory)
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
