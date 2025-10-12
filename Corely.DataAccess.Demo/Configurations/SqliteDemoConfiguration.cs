using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

public class SqliteDemoConfiguration : EFSqliteConfigurationBase
{
    private readonly SqliteConnection _connection;

    public SqliteDemoConfiguration(string connectionString)
        : base(connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connection);
    }
}
