using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Entities;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Mock.Repos;

public class MockRepo<TEntity> : IRepo<TEntity>
    where TEntity : class
{
    public readonly List<TEntity> Entities = [];

    public MockRepo()
        : base() { }

    private static bool TryGetId(object entity, out object? id)
    {
        id = null;
        if (entity == null)
            return false;

        var idInterface = entity
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasIdPk<>)
            );

        if (idInterface == null)
            return false;

        var prop = idInterface.GetProperty("Id");
        if (prop == null)
            return false;

        id = prop.GetValue(entity);
        return id != null;
    }

    private static object? GetIdOrNull(object entity) => TryGetId(entity, out var id) ? id : null;

    private static bool IsCreatedUtcUnset(object entity) =>
        entity is IHasCreatedUtc hc && hc.CreatedUtc == default;

    private static void EnsureCreatedUtc(object entity)
    {
        if (entity is IHasCreatedUtc hc && hc.CreatedUtc == default)
        {
            hc.CreatedUtc = DateTime.UtcNow;
        }
    }

    public virtual Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        EnsureCreatedUtc(entity);
        Entities.Add(entity);
        return Task.FromResult(entity);
    }

    public virtual Task CreateAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var e in entities)
            EnsureCreatedUtc(e);
        Entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    )
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

    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var predicate = query.Compile();
        return Task.FromResult(Entities.Any(predicate));
    }

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? query = null,
        CancellationToken cancellationToken = default
    )
    {
        if (query == null)
        {
            return Task.FromResult(Entities.Count);
        }
        var predicate = query.Compile();
        return Task.FromResult(Entities.Count(predicate));
    }

    public virtual Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    )
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

        var incomingId = GetIdOrNull(entity);
        if (incomingId != null)
        {
            // Find existing by key (supports value types)
            for (int i = 0; i < Entities.Count; i++)
            {
                var existingId = GetIdOrNull(Entities[i]!);
                if (existingId != null && Equals(existingId, incomingId))
                {
                    // Preserve CreatedUtc if update entity did not set it
                    if (
                        Entities[i] is IHasCreatedUtc existingCreated
                        && entity is IHasCreatedUtc incomingCreated
                        && IsCreatedUtcUnset(incomingCreated)
                    )
                    {
                        incomingCreated.CreatedUtc = existingCreated.CreatedUtc;
                    }
                    Entities[i] = entity; // replace reference
                    return Task.CompletedTask;
                }
            }
        }

        // Fallback to reference equality if no key interface implemented or not found
        var index = Entities.FindIndex(e => ReferenceEquals(e, entity));
        if (index > -1)
        {
            Entities[index] = entity;
        }
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var incomingId = GetIdOrNull(entity);
        if (incomingId != null)
        {
            var toRemove = Entities.FirstOrDefault(e => Equals(GetIdOrNull(e!), incomingId));
            if (toRemove != null)
            {
                Entities.Remove(toRemove);
                return Task.CompletedTask;
            }
        }
        Entities.Remove(entity); // reference fallback
        return Task.CompletedTask;
    }

    public virtual Task<TResult> EvaluateAsync<TResult>(
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> run,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(run);
        var queryable = Entities.AsQueryable();
        return run(queryable, cancellationToken);
    }

    public virtual Task<List<TResult>> QueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> build,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(build);
        var queryable = Entities.AsQueryable();
        var shaped = build(queryable);
        return Task.FromResult(shaped.ToList());
    }
}
