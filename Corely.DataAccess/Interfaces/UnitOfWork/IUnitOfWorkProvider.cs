namespace Corely.DataAccess.Interfaces.UnitOfWork;

public interface IUnitOfWorkProvider
{
    Task BeginAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
