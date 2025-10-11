using Corely.Common.Models;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.EntityFramework.UnitOfWork;

internal class EFUoWProvider : DisposeBase, IUnitOfWorkProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EFUnitOfWorkScope _scope = new();
    private readonly Dictionary<DbContext, IDbContextTransaction?> _transactions = [];

    public EFUoWProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _scope.ContextRegistered += OnContextRegistered;
    }

    private async void OnContextRegistered(DbContext context)
    {
        if (!_scope.IsActive)
            return;

        // Late enlistment: if active and this context not tracked yet, start a transaction when provider supports it
        if (!_transactions.ContainsKey(context))
        {
            var supportsTx =
                context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            if (supportsTx)
            {
                // Fire-and-forget: exceptions here will surface on awaiters of Commit/Rollback; this is acceptable for unit of work boundaries.
                var tx = await context.Database.BeginTransactionAsync();
                _transactions[context] = tx;
            }
            else
            {
                _transactions[context] = null;
            }
        }
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_scope.IsActive)
            throw new InvalidOperationException("Unit of work has already begun.");

        _scope.IsActive = true;

        _transactions.Clear();
        foreach (var ctx in _scope.Contexts)
        {
            var supportsTx = ctx.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            _transactions[ctx] = supportsTx
                ? await ctx.Database.BeginTransactionAsync(cancellationToken)
                : null;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!_scope.IsActive)
            throw new InvalidOperationException("No active unit of work to commit.");

        try
        {
            foreach (var ctx in _scope.Contexts)
            {
                if (ctx.ChangeTracker.HasChanges())
                    await ctx.SaveChangesAsync(cancellationToken);
            }

            foreach (var kv in _transactions)
            {
                if (kv.Value != null)
                {
                    await kv.Value.CommitAsync(cancellationToken);
                    await kv.Value.DisposeAsync();
                }
            }
        }
        finally
        {
            _transactions.Clear();
            _scope.IsActive = false;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!_scope.IsActive)
            throw new InvalidOperationException("No active unit of work to roll back.");

        try
        {
            foreach (var kv in _transactions)
            {
                if (kv.Value != null)
                {
                    await kv.Value.RollbackAsync(cancellationToken);
                    await kv.Value.DisposeAsync();
                }
            }

            foreach (var ctx in _scope.Contexts)
            {
                ctx.ChangeTracker.Clear();
            }
        }
        finally
        {
            _transactions.Clear();
            _scope.IsActive = false;
        }
    }

    public IRepo<TEntity> GetRepository<TEntity>()
        where TEntity : class
    {
        var repo = _serviceProvider.GetRequiredService<IRepo<TEntity>>();
        if (repo is IEFScopeContextSetter repoWithScope)
            repoWithScope.SetScope(_scope);
        return repo;
    }

    protected override void DisposeManagedResources()
    {
        foreach (var tx in _transactions.Values)
        {
            tx?.Dispose();
        }
        _transactions.Clear();
        _scope.IsActive = false;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var tx in _transactions.Values)
        {
            if (tx != null)
                await tx.DisposeAsync();
        }
        _transactions.Clear();
        _scope.IsActive = false;
    }
}
