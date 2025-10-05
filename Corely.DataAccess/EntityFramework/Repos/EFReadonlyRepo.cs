using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Threading;

namespace Corely.DataAccess.EntityFramework.Repos;

public class EFReadonlyRepo<TEntity>
    : IReadonlyRepo<TEntity>
    where TEntity : class
{
    protected readonly ILogger<EFReadonlyRepo<TEntity>> Logger;
    protected readonly DbSet<TEntity> DbSet;

    public EFReadonlyRepo(
        ILogger<EFReadonlyRepo<TEntity>> logger,
        DbContext context)
    {
        Logger = logger.ThrowIfNull(nameof(logger));
        DbSet = context.Set<TEntity>().ThrowIfNull(nameof(context));
        Logger.LogDebug("{RepoType} created for {EntityType}", GetType().Name.Split('`')[0], typeof(TEntity).Name);
    }

    public virtual async Task<TEntity?> GetAsync(
       Expression<Func<TEntity, bool>> query,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var queryable = DbSet.AsQueryable();

        if (include != null)
        {
            queryable = include(queryable);
        }

        if (orderBy != null)
        {
            queryable = orderBy(queryable);
        }

        return await queryable.FirstOrDefaultAsync(query, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await DbSet.AnyAsync(query, cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = DbSet.AsQueryable();

        if (include != null)
        {
            queryable = include(queryable);
        }

        if (orderBy != null)
        {
            queryable = orderBy(queryable);
        }

        if (query != null)
        {
            queryable = queryable.Where(query);
        }

        return await queryable.ToListAsync(cancellationToken);
    }
}
