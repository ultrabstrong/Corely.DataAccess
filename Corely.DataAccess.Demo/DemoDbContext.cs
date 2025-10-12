using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.Demo;

internal class DemoDbContext : DbContextBase
{
    public DemoDbContext(IEFConfiguration efConfiguration)
        : base(efConfiguration) { }

    public DemoDbContext(DbContextOptions<DemoDbContext> options, IEFConfiguration efConfiguration)
        : base(options, efConfiguration) { }

    public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();
}
