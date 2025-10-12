using Corely.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFMySqlConfigurationBase : IEFConfiguration
{
    private class EFDbTypes : IEFDbTypes
    {
        public string UTCDateColumnType => "TIMESTAMP";
        public string UTCDateColumnDefaultValue => "(UTC_TIMESTAMP)";
    }

    private readonly EFDbTypes _efDbTypes = new();
    protected readonly string connectionString;

    public EFMySqlConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString.ThrowIfNull(nameof(connectionString));
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public IEFDbTypes GetDbTypes() => _efDbTypes;
}
