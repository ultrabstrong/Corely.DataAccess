using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Mock.Repos;

public class MockReadonlyRepo<TEntity> : IReadonlyRepo<TEntity>
    where TEntity : class
{
    private readonly MockRepo<TEntity> _mockRepo;

    public MockReadonlyRepo(IRepo<TEntity> mockRepo)
    {
        // Use the same Entities list for all mocks to simulate a single data store
        _mockRepo = (MockRepo<TEntity>)mockRepo;
    }

    public virtual async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> query,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => await _mockRepo.GetAsync(query, orderBy, include, cancellationToken);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> query,
        CancellationToken cancellationToken = default
    ) => await _mockRepo.AnyAsync(query, cancellationToken);

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? query = null,
        CancellationToken cancellationToken = default
    ) => await _mockRepo.CountAsync(query, cancellationToken);

    public virtual async Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default
    ) => await _mockRepo.ListAsync(query, orderBy, include, cancellationToken);

    public virtual Task<TResult> EvaluateAsync<TResult>(
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> run,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(run);
        var queryable = _mockRepo.Entities.AsQueryable();
        return run(queryable, cancellationToken);
    }

    public virtual Task<List<TResult>> QueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> build,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(build);
        var queryable = _mockRepo.Entities.AsQueryable();
        var shaped = build(queryable);
        return Task.FromResult(shaped.ToList());
    }
}
