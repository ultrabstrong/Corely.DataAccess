using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

public class MySqlDemoConfiguration(string connectionString)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        // Oracle's provider resolves server capabilities from the connection; unlike Pomelo it has
        // no ServerVersion to declare up front.
        optionsBuilder.UseMySQL(connectionString);
    }
}
