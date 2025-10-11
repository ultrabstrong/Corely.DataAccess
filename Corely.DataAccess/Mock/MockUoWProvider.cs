using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;

namespace Corely.DataAccess.Mock;

public class MockUoWProvider : IUnitOfWorkProvider
{
    public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public IRepo<TEntity> GetRepository<TEntity>()
        where TEntity : class =>
        throw new NotSupportedException(
            "MockUoWProvider does not resolve repositories. Use DI to provide repos if needed."
        );
}
