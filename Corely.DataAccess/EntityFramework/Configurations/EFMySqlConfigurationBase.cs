using Corely.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFMySqlConfigurationBase : IEFConfiguration
{
    private readonly MySqlDbTypes _dbTypes = new();
    protected readonly string connectionString;

    public EFMySqlConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString.ThrowIfNull(nameof(connectionString));
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public virtual IDbTypes GetDbTypes() => _dbTypes;
}
