using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.Demo;

// Subclass EFReadonlyRepo so DI binding to DemoDbContext works
public sealed class DemoReadonlyRepo<TEntity>(
    ILogger<EFReadonlyRepo<TEntity>> logger,
    DemoDbContext dbContext) : EFReadonlyRepo<TEntity>(logger, dbContext), IReadonlyRepo<TEntity>
    where TEntity : class
{
}
