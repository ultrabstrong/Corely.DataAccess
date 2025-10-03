using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.Interfaces.UnitOfWork;

namespace Corely.DataAccess.Demo;

// Subclass EFUoWProvider so DI binding to DemoDbContext works
public sealed class DemoUoWProvider(DemoDbContext dbContext)
    : EFUoWProvider(dbContext), IUnitOfWorkProvider
{
}
