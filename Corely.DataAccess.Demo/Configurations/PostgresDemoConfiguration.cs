using System.Reflection;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

public class PostgresDemoConfiguration(string connectionString)
    : EFPostgresConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        var migrationsAssembly = Assembly.GetExecutingAssembly().GetName().Name;
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
    }
}
