using System.Linq.Expressions;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.EntityFramework.Repos;

internal sealed class EFRepoAdapter<TEntity> : IRepo<TEntity>, IEFScopeContextSetter
    where TEntity : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEFContextResolver _entityMapper;
    private readonly Lazy<IRepo<TEntity>> _concrete; // EFRepo<TContext,TEntity>

    public EFRepoAdapter(IServiceProvider serviceProvider, IEFContextResolver entityMapper)
    {
        _serviceProvider = serviceProvider;
        _entityMapper = entityMapper;
        _concrete = new Lazy<IRepo<TEntity>>(() =>
        {
            var ctxType = _entityMapper.GetContextTypeFor(typeof(TEntity));
            var concrete = typeof(EFRepo<,>).MakeGenericType(ctxType, typeof(TEntity));
            return (IRepo<TEntity>)_serviceProvider.GetRequiredService(concrete);
        });
    }

    private IRepo<TEntity> Repo => _concrete.Value;

    public Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    ) => Repo.CreateAsync(entity, cancellationToken);

    public Task CreateAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    ) => Repo.CreateAsync(entities, cancellationToken);

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Repo.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Repo.DeleteAsync(entity, cancellationToken);

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => Repo.GetAsync(query, orderBy, include, cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> query,
        CancellationToken cancellationToken = default
    ) => Repo.AnyAsync(query, cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? query = null,
        CancellationToken cancellationToken = default
    ) => Repo.CountAsync(query, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => Repo.ListAsync(query, orderBy, include, cancellationToken);

    public void SetScope(EFUnitOfWorkScope scope)
    {
        if (_concrete.Value is IEFScopeContextSetter setter)
        {
            setter.SetScope(scope);
            return;
        }
        throw new InvalidOperationException(
            $"Resolved concrete repo '{_concrete.Value.GetType().FullName}' does not implement {nameof(IEFScopeContextSetter)}."
        );
    }
}
