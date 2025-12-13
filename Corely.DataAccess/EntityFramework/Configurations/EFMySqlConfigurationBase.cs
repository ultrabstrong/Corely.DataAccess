using Corely.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFMySqlConfigurationBase : IEFConfiguration
{
    private class EFDbTypes : IEFDbTypes
    {
        public string UTCDateColumnType => "TIMESTAMP";
        public string UTCDateColumnDefaultValue => "(UTC_TIMESTAMP)";
        public string UuidColumnType => "CHAR(36)";
        public string UuidColumnDefaultValue => "(UUID())";
        public string JsonColumnType => "JSON";
        public string BoolColumnType => "TINYINT(1)";
        public string DecimalColumnType => "DECIMAL(19,4)";
        public string DecimalColumnDefaultValue => "0";
        public string BigIntColumnType => "BIGINT";
    }

    private readonly EFDbTypes _efDbTypes = new();
    protected readonly string connectionString;

    public EFMySqlConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString.ThrowIfNull(nameof(connectionString));
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public virtual IEFDbTypes GetDbTypes() => _efDbTypes;
}
