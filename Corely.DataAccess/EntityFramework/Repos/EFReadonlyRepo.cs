using System.Linq.Expressions;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.EntityFramework.Repos;

internal class EFReadonlyRepo<TContext, TEntity> : IReadonlyRepo<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    protected readonly ILogger<EFReadonlyRepo<TContext, TEntity>> Logger;
    protected readonly TContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public EFReadonlyRepo(ILogger<EFReadonlyRepo<TContext, TEntity>> logger, TContext context)
    {
        Logger = logger.ThrowIfNull(nameof(logger));
        DbContext = context.ThrowIfNull(nameof(context));
        DbSet = context.Set<TEntity>().ThrowIfNull(nameof(context));
        Logger.LogTrace(
            "{RepoType} created for {EntityType} on {ContextType}",
            GetType().Name.Split('`')[0],
            typeof(TEntity).Name,
            typeof(TContext).Name
        );
    }

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var queryable = DbSet.AsQueryable();
        if (include != null)
            queryable = include(queryable);
        if (orderBy != null)
            queryable = orderBy(queryable);
        return queryable.FirstOrDefaultAsync(query, cancellationToken);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> query,
        CancellationToken cancellationToken = default
    ) => DbSet.AnyAsync(query, cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? query = null,
        CancellationToken cancellationToken = default
    ) =>
        query == null
            ? DbSet.CountAsync(cancellationToken)
            : DbSet.CountAsync(query, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    )
    {
        var queryable = DbSet.AsQueryable();
        if (include != null)
            queryable = include(queryable);
        if (orderBy != null)
            queryable = orderBy(queryable);
        if (query != null)
            queryable = queryable.Where(query);
        return queryable.ToListAsync(cancellationToken);
    }
}
