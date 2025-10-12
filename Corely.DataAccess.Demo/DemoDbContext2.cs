using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo;

internal class DemoDbContext2 : DbContextBase
{
    public DemoDbContext2(IEFConfiguration efConfiguration)
        : base(efConfiguration) { }

    public DemoDbContext2(
        DbContextOptions<DemoDbContext2> options,
        IEFConfiguration efConfiguration
    )
        : base(options, efConfiguration) { }

    public DbSet<DemoEntity2> DemoEntities => Set<DemoEntity2>();
}
