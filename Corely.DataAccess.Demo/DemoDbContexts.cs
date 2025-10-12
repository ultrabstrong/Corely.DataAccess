using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo;

internal class DemoDbContext : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public DemoDbContext(IEFConfiguration efConfiguration)
        : base() => _efConfiguration = efConfiguration;

    public DemoDbContext(DbContextOptions<DemoDbContext> options, IEFConfiguration efConfiguration)
        : base(options) => _efConfiguration = efConfiguration;

    public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _efConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DemoEntityConfiguration(_efConfiguration.GetDbTypes()));
    }
}

internal class DemoDbContext2 : DbContext
{
    private readonly IEFConfiguration _efConfiguration;

    public DemoDbContext2(IEFConfiguration efConfiguration)
        : base() => _efConfiguration = efConfiguration;

    public DemoDbContext2(
        DbContextOptions<DemoDbContext2> options,
        IEFConfiguration efConfiguration
    )
        : base(options) => _efConfiguration = efConfiguration;

    public DbSet<DemoEntity2> DemoEntities2 => Set<DemoEntity2>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _efConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new DemoEntity2Configuration(_efConfiguration.GetDbTypes())
        );
    }
}
