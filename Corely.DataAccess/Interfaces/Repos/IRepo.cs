using System.Linq.Expressions;

namespace Corely.DataAccess.Interfaces.Repos;

public interface IRepo<TEntity> : IReadonlyRepo<TEntity>
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a set-based update to every entity matching <paramref name="query"/> in a single
    /// database round-trip, returning the number of rows affected.
    /// </summary>
    /// <remarks>
    /// This bypasses the change tracker, so <see cref="Interfaces.Entities.IHasModifiedUtc"/> is
    /// NOT applied automatically - set it explicitly in <paramref name="setProperties"/> when needed.
    /// </remarks>
    Task<int> ExecuteUpdateAsync(
        Expression<Func<TEntity, bool>> query,
        Action<IUpdateSetters<TEntity>> setProperties,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
