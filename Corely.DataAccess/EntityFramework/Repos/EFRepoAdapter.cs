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
    private readonly Lazy<object> _repoResolver; // holds EFRepo<TContext,TEntity>

    public EFRepoAdapter(IServiceProvider serviceProvider, IEFContextResolver entityMapper)
    {
        _serviceProvider = serviceProvider;
        _entityMapper = entityMapper;
        _repoResolver = new Lazy<object>(() =>
        {
            var ctxType = _entityMapper.GetContextTypeFor(typeof(TEntity));
            var concrete = typeof(EFRepo<,>).MakeGenericType(ctxType, typeof(TEntity));
            return _serviceProvider.GetRequiredService(concrete);
        });
    }

    private dynamic RepoResolver => _repoResolver.Value;

    public Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    ) => RepoResolver.CreateAsync(entity, cancellationToken);

    public Task CreateAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    ) => RepoResolver.CreateAsync(entities, cancellationToken);

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        RepoResolver.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        RepoResolver.DeleteAsync(entity, cancellationToken);

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => RepoResolver.GetAsync(query, orderBy, include, cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> query,
        CancellationToken cancellationToken = default
    ) => RepoResolver.AnyAsync(query, cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? query = null,
        CancellationToken cancellationToken = default
    ) => RepoResolver.CountAsync(query, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => RepoResolver.ListAsync(query, orderBy, include, cancellationToken);

    public void SetScope(EFUnitOfWorkScope scope) => RepoResolver.SetScope(scope);
}
