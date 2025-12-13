using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFInMemoryConfigurationBase : IEFConfiguration
{
    private readonly InMemoryDbTypes _dbTypes = new();

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public virtual IDbTypes GetDbTypes() => _dbTypes;
}
