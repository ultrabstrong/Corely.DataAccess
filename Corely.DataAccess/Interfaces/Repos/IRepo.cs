namespace Corely.DataAccess.Interfaces.Repos;

public interface IRepo<TEntity>
    : IReadonlyRepo<TEntity>
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task CreateAsync(params TEntity[] entities);

    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, Func<TEntity, bool> query, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
