using Corely.Common.Models;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Corely.DataAccess.EntityFramework;

internal class EFUoWProvider : DisposeBase, IUnitOfWorkProvider
{
    private readonly DbContext _dbContext;
    private readonly bool _supportTransactions;
    private readonly IUnitOfWorkScopeAccessor _scope;
    private IDbContextTransaction? _transaction;

    public EFUoWProvider(DbContext dbContext, IUnitOfWorkScopeAccessor scope)
    {
        _dbContext = dbContext;
        _scope = scope;
        _supportTransactions = dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (!_scope.IsActive)
        {
            _scope.IsActive = true;
        }
        if (_transaction == null && _supportTransactions)
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_scope.IsActive || _transaction != null)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        finally
        {
            if (_scope.IsActive)
            {
                _scope.IsActive = false;
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            else
            {
                _dbContext.ChangeTracker.Clear();
            }
        }
        finally
        {
            if (_scope.IsActive)
            {
                _scope.IsActive = false;
            }
        }
    }

    protected override void DisposeManagedResources()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
        _scope.IsActive = false;
    }

    protected async override ValueTask DisposeAsyncCore()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
        _scope.IsActive = false;
    }
}
