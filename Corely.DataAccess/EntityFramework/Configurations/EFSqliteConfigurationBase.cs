using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFSqliteConfigurationBase : IEFConfiguration
{
    private class EFDbTypes : IEFDbTypes
    {
        // SQLite stores dates as TEXT/NUMERIC; use TEXT with CURRENT_TIMESTAMP default for demo/common cases
        public string UTCDateColumnType => "TEXT";
        public string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";
    }

    private readonly EFDbTypes _efDbTypes = new();
    protected readonly string connectionString;

    public EFSqliteConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public IEFDbTypes GetDbTypes() => _efDbTypes;
}
