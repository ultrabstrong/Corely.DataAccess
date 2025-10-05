using Corely.DataAccess.Interfaces.UnitOfWork;

namespace Corely.DataAccess.Mock;

public class MockUoWProvider : IUnitOfWorkProvider
{
    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
