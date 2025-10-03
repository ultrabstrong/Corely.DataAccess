using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo.Configurations;

public class InMemoryDemoConfiguration(string databaseName) : EFInMemoryConfigurationBase
{
    private readonly string _dbName = databaseName;
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(_dbName);
    }
}
