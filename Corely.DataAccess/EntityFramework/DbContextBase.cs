using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework;

public abstract class DbContextBase : DbContext
{
    protected readonly IEFConfiguration efConfiguration;

    public DbContextBase(IEFConfiguration efConfiguration)
        : base()
    {
        this.efConfiguration = efConfiguration;
    }

    public DbContextBase(DbContextOptions<DbContextBase> opts, IEFConfiguration efConfiguration)
        : base(opts)
    {
        this.efConfiguration = efConfiguration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        efConfiguration.Configure(optionsBuilder);
    }
}
