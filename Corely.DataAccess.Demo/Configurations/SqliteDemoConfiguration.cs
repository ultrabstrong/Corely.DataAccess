using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

internal sealed class SqliteDemoConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection? _sqliteConnection;

    public SqliteDemoConfiguration(
        string connectionString = "Data Source=docstodata;Mode=Memory;Cache=Shared"
    )
        : base(connectionString)
    {
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
