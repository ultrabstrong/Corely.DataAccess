using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.EntityFramework.Repos;

internal interface IEFRepoContract<TEntity> : IReadonlyRepo<TEntity>, IEFScopeContextSetter
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
