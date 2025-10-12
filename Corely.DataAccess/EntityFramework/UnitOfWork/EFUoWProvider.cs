using Corely.Common.Models;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Corely.DataAccess.EntityFramework.UnitOfWork;

internal class EFUoWProvider : DisposeBase, IUnitOfWorkProvider
{
    private readonly HashSet<DbContext> _contexts = [];
    private readonly Dictionary<DbContext, IDbContextTransaction?> _transactions = [];
    private bool _isActive;

    public bool IsActive => _isActive;

    public void Register(DbContext context)
    {
        if (context == null)
            return;

        if (_contexts.Add(context) && _isActive)
        {
            var supportsTx =
                context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            _transactions[context] = supportsTx ? context.Database.BeginTransaction() : null;
        }
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_isActive)
            throw new InvalidOperationException("Unit of work has already begun.");

        _isActive = true;

        _transactions.Clear();
        foreach (var ctx in _contexts)
        {
            var supportsTx = ctx.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            _transactions[ctx] = supportsTx
                ? await ctx.Database.BeginTransactionAsync(cancellationToken)
                : null;
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
            throw new InvalidOperationException("No active unit of work to commit.");

        try
        {
            // Ensure any newly registered contexts have a transaction if supported
            foreach (var ctx in _contexts)
            {
                var supportsTx =
                    ctx.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
                if (supportsTx && !_transactions.ContainsKey(ctx))
                {
                    _transactions[ctx] = await ctx.Database.BeginTransactionAsync(
                        cancellationToken
                    );
                }
            }

            // Repos now save as they go; just commit/cleanup transactions here
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
            _isActive = false;
            _contexts.Clear();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!_isActive)
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

            foreach (var ctx in _contexts)
            {
                ctx.ChangeTracker.Clear();
            }
        }
        finally
        {
            _transactions.Clear();
            _isActive = false;
            _contexts.Clear();
        }
    }

    protected override void DisposeManagedResources()
    {
        foreach (var tx in _transactions.Values)
        {
            tx?.Dispose();
        }
        _transactions.Clear();
        _contexts.Clear();
        _isActive = false;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var tx in _transactions.Values)
        {
            if (tx != null)
                await tx.DisposeAsync();
        }
        _transactions.Clear();
        _contexts.Clear();
        _isActive = false;
    }
}
