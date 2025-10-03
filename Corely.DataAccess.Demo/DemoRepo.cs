using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.Demo;

// Subclass EFRepo so DI binding to DemoDbContext works
public sealed class DemoRepo<TEntity>(
    ILogger<EFRepo<TEntity>> logger,
    DemoDbContext dbContext) : EFRepo<TEntity>(logger, dbContext), IRepo<TEntity>
    where TEntity : class
{
}
