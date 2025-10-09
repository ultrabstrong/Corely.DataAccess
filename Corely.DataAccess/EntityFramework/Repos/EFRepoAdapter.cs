using Corely.DataAccess.Interfaces.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.EntityFramework.Repos;

internal sealed class EFRepoAdapter<TEntity> : IRepo<TEntity>
    where TEntity : class
{
    private readonly IServiceProvider _sp;
    private readonly IEntityContextMap _map;
    private readonly Lazy<object> _inner; // holds EFRepo<TContext,TEntity>

    public EFRepoAdapter(IServiceProvider sp, IEntityContextMap map)
    {
        _sp = sp;
        _map = map;
        _inner = new Lazy<object>(() => ResolveInner());
    }

    private object ResolveInner()
    {
        var ctxType = _map.GetContextTypeFor(typeof(TEntity));
        var concrete = typeof(EFRepo<,>).MakeGenericType(ctxType, typeof(TEntity));
        return _sp.GetRequiredService(concrete);
    }

    private dynamic Inner => _inner.Value;

    public Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default) => Inner.CreateAsync(entity, cancellationToken);
    public Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => Inner.CreateAsync(entities, cancellationToken);
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => Inner.UpdateAsync(entity, cancellationToken);
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) => Inner.DeleteAsync(entity, cancellationToken);
    public Task<TEntity?> GetAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default) => Inner.GetAsync(query, orderBy, include, cancellationToken);
    public Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> query, CancellationToken cancellationToken = default) => Inner.AnyAsync(query, cancellationToken);
    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>>? query = null, CancellationToken cancellationToken = default) => Inner.CountAsync(query, cancellationToken);
    public Task<List<TEntity>> ListAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>>? query = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default) => Inner.ListAsync(query, orderBy, include, cancellationToken);
}

