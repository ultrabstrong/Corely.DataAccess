using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.DataAccess.EntityFramework.Repos;

internal sealed class EFReadonlyRepoAdapter<TEntity> : IReadonlyRepo<TEntity>
    where TEntity : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEFContextResolver _entityMapper;
    private readonly Lazy<IReadonlyRepo<TEntity>> _repo; // EFReadonlyRepo<TContext,TEntity> via contract

    public EFReadonlyRepoAdapter(IServiceProvider serviceProvider, IEFContextResolver entityMapper)
    {
        _serviceProvider = serviceProvider;
        _entityMapper = entityMapper;
        _repo = new Lazy<IReadonlyRepo<TEntity>>(() =>
        {
            var ctxType = _entityMapper.GetContextTypeFor(typeof(TEntity));
            var concrete = typeof(EFReadonlyRepo<,>).MakeGenericType(ctxType, typeof(TEntity));
            return (IReadonlyRepo<TEntity>)_serviceProvider.GetRequiredService(concrete);
        });
    }

    private IReadonlyRepo<TEntity> Repo => _repo.Value;

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
}
