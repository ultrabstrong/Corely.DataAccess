using Corely.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Configurations;

public abstract class EFSqliteConfigurationBase : IEFConfiguration
{
    private class EFDbTypes : IEFDbTypes
    {
        // SQLite stores dates as TEXT/NUMERIC; use TEXT with CURRENT_TIMESTAMP default for demo/common cases
        public string UTCDateColumnType => "TEXT";
        public string UTCDateColumnDefaultValue => "CURRENT_TIMESTAMP";

        // SQLite has no native UUID type; store as TEXT
        public string UuidColumnType => "TEXT";
        public string UuidColumnDefaultValue =>
            "(lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))))";

        // SQLite has no native JSON type; store as TEXT
        public string JsonColumnType => "TEXT";

        // SQLite has no native BOOLEAN type; store as INTEGER (0/1)
        public string BoolColumnType => "INTEGER";

        // SQLite has no native DECIMAL type; store as TEXT for precision or REAL for performance
        public string DecimalColumnType => "TEXT";
        public string DecimalColumnDefaultValue => "'0'";

        // SQLite uses INTEGER for all integer types (dynamically sized)
        public string BigIntColumnType => "INTEGER";
    }

    private readonly EFDbTypes _efDbTypes = new();
    protected readonly string connectionString;

    public EFSqliteConfigurationBase(string connectionString)
    {
        this.connectionString = connectionString.ThrowIfNull(nameof(connectionString));
    }

    public abstract void Configure(DbContextOptionsBuilder optionsBuilder);

    public virtual IEFDbTypes GetDbTypes() => _efDbTypes;
}
