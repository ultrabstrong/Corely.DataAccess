using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.Fixtures;

public class AnotherDbContextFixture : DbContext
{
    public AnotherDbContextFixture(DbContextOptions<AnotherDbContextFixture> options) : base(options) { }

    public DbSet<EntityFixture> Entities { get; set; } = null!;
}
