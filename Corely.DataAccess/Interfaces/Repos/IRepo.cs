using System.Linq.Expressions;

namespace Corely.DataAccess.Interfaces.Repos;

public interface IRepo<TEntity> : IReadonlyRepo<TEntity>
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    // Bypasses the change tracker: set ModifiedUtc yourself.
    Task<int> ExecuteUpdateAsync(
        Expression<Func<TEntity, bool>> query,
        Action<IUpdateSetters<TEntity>> setProperties,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
