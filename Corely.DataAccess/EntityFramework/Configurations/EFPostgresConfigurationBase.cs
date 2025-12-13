using Corely.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFPostgresConfigurationBase : IEFConfiguration
{
    private class EFDbTypes : IEFDbTypes
    {
        public string UTCDateColumnType => "TIMESTAMP";
        public string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";
        public string UuidColumnType => "UUID";
        public string UuidColumnDefaultValue => "gen_random_uuid()";
        public string JsonColumnType => "JSONB";
        public string BoolColumnType => "BOOLEAN";
        public string DecimalColumnType => "NUMERIC(19,4)";
        public string DecimalColumnDefaultValue => "0";
        public string BigIntColumnType => "BIGINT";
    }

    private readonly EFDbTypes _efDbTypes = new();
    protected readonly string connectionString;

    public EFPostgresConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString.ThrowIfNull(nameof(connectionString));
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public virtual IEFDbTypes GetDbTypes() => _efDbTypes;
}
