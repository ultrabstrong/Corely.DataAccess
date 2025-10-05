using Corely.DataAccess.Interfaces.Entities;
using Corely.DataAccess.Interfaces.Repos;
using System.Linq.Expressions;
using System.Threading; // added

namespace Corely.DataAccess.Mock.Repos;

public class MockRepo<TEntity>
    : IRepo<TEntity>
    where TEntity : class
{
    public readonly List<TEntity> Entities = [];

    public MockRepo() : base() { }

    public virtual Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities.Add(entity);
        return Task.FromResult(entity);
    }

    public virtual Task CreateAsync(params TEntity[] entities)
    {
        Entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public virtual Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var predicate = query.Compile();
        var queryable = Entities.AsQueryable();

        if (include != null)
        {
            queryable = include(queryable);
        }

        if (orderBy != null)
        {
            queryable = orderBy(queryable);
        }

        return await Task.FromResult(queryable.FirstOrDefault(predicate));
    }

    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var predicate = query.Compile();
        return Task.FromResult(Entities.Any(predicate));
    }

    public virtual Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = Entities.AsQueryable();
        if (query != null)
        {
            var predicate = query.Compile();
            queryable = queryable.Where(predicate).AsQueryable();
        }
        if (include != null)
        {
            queryable = include(queryable);
        }
        if (orderBy != null)
        {
            queryable = orderBy(queryable);
        }
        return Task.FromResult(queryable.ToList());
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (typeof(IHasModifiedUtc).IsAssignableFrom(typeof(TEntity)))
        {
            ((IHasModifiedUtc)entity).ModifiedUtc = DateTime.UtcNow;
        }

        var index = Entities.FindIndex(e => ReferenceEquals(e, entity));
        if (index > -1)
        {
            Entities[index] = entity;
        }
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities.Remove(entity);
        return Task.CompletedTask;
    }
}
