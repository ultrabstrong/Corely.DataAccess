# Repositories

Generic repository abstractions simplify common data access patterns while remaining extensible.

## Interfaces
```csharp
public interface IReadonlyRepo<TEntity>
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity,bool>> query, ...);
    Task<bool> AnyAsync(Expression<Func<TEntity,bool>> query);
    Task<int> CountAsync(Expression<Func<TEntity,bool>>? query = null);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity,bool>>? query = null, ...);

    // Advanced queries
    // EvaluateAsync: run arbitrary aggregate/single-result operations server-side
    Task<TResult> EvaluateAsync<TResult>(Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> run, CancellationToken ct = default);

    // QueryAsync: shape to DTOs/projections and materialize as list
    Task<List<TResult>> QueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> build, CancellationToken ct = default);
}

public interface IRepo<TEntity> : IReadonlyRepo<TEntity>
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);
    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}
```

## Registration
Use the provided helper to wire repositories and the unit of work:
```csharp
services.RegisterEntityFrameworkReposAndUoW();
```
Requirements:
- Register each DbContext in DI (AddDbContext<...>).
- Register a single IEFConfiguration for your contexts (see [Configurations](configurations.md)).

Mock providers for unit tests:
```csharp
services.RegisterMockReposAndUoW();
```

## Behavior
- For EF, repositories automatically resolve and use the appropriate DbContext for each entity at runtime. If none or multiple contexts match, an error is thrown.
- All write operations participate in an active Unit of Work when one is begun; otherwise changes are saved immediately.

## Query Customization
All repository methods support:
- include: Func<IQueryable<TEntity>, IQueryable<TEntity>>
- orderBy: Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>
- optional predicate (for Get/Any/Count/List)

Example:
```csharp
var recent = await repo.ListAsync(
    e => e.IsActive,
    orderBy: q => q.OrderByDescending(x => x.CreatedUtc),
    include: q => q.Include(x => x.Children));
```

## Advanced Queries
Use the generic methods to avoid growing the interface with method-per-aggregate overloads:

- Aggregates and single result:
```csharp
var total = await repo.EvaluateAsync((q, ct) => q.SumAsync(e => e.Amount, ct));
var anyHigh = await repo.EvaluateAsync((q, ct) => q.AnyAsync(e => e.Amount > 100, ct));
var maxId = await repo.EvaluateAsync((q, ct) => q.MaxAsync(e => e.Id, ct));
```

- Projections:
```csharp
var items = await repo.QueryAsync(q =>
    q.Where(e => e.IsActive)
     .OrderBy(e => e.Id)
     .Select(e => new ItemDto { Id = e.Id, Name = e.Name }));
```

- Paging (Skip/Take):
```csharp
// Deterministic paging requires ordering
var pageIndex = 1; // zero-based
var pageSize = 10;
var page = await repo.QueryAsync(q =>
    q.OrderBy(e => e.Id)
     .Skip(pageIndex * pageSize)
     .Take(pageSize)
     .Select(e => new ItemDto { Id = e.Id, Name = e.Name }));
```

These run server-side when using EF-backed repos; mocks execute over in-memory collections.

## Create/Update/Delete Semantics
- CreateAsync: single or batch; inside an active UoW, changes are deferred until CommitAsync (unless using in-Memory db).
- UpdateAsync: updates tracked entity by key; sets ModifiedUtc when IHasModifiedUtc.
- DeleteAsync: removes by reference/key.

## Unit of Work Integration
See [Unit of Work](unit-of-work.md). In short:
- BeginAsync activates the scope; repositories defer SaveChanges while active.
- CommitAsync persists changes for all enlisted contexts and commits transactions where supported.
- RollbackAsync rolls back transactions where present and clears ChangeTrackers.

## When to Subclass
Stick with the provided repos unless you need:
- Domain-specific query helpers
- Performance tweaks (compiled queries, projections)
- Raw SQL / stored procedures behind a safe API
- Cross-cutting behavior (soft delete, multi-tenancy, policies)

For tests that shouldn’t depend on EF Core semantics, use mock providers.
