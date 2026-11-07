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

    // Set-based update: one statement for every matching row
    Task<int> ExecuteUpdateAsync(Expression<Func<TEntity,bool>> query, Action<IUpdateSetters<TEntity>> setProperties, CancellationToken ct = default);

    // Set-based delete: one statement for every matching row
    Task<int> ExecuteDeleteAsync(Expression<Func<TEntity,bool>> query, CancellationToken ct = default);
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
- All write operations save immediately. While a Unit of Work is active they run inside its transaction, which CommitAsync commits and RollbackAsync rolls back.

## Scopes and Fresh Reads
One DI scope is one unit of work. Repositories share the scope's DbContext, and EF remembers every row that context has loaded: a later read of the same row in the same scope returns the remembered instance, not what the database holds now. This is what lets you read entities, change them across several calls and save them, so it is by design and the library does not refresh it.

For data as the database holds it now, read it in a new scope. In a request based host (MVC, APIs, Functions, a hosted service per message) every request already gets one. In Blazor Server a scope lives as long as the circuit, or as long as the component with `OwningComponentBase`, so create a scope per operation instead:
```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var jobs = scope.ServiceProvider.GetRequiredService<IReadonlyRepo<JobEntity>>();
_job = await jobs.GetAsync(j => j.Id == jobId);
```
Don't work around a long lived scope with `AsNoTracking()` at the call site: it fixes one read and leaves every other read in that scope stale.

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
- CreateAsync: single or batch; saved at once (inside the unit of work's transaction when one is active), so database generated keys are set when it returns.
- UpdateAsync: if the context already tracks an entity with the same key, copies the given values onto it and saves only what changed; otherwise attaches the given entity and writes every column. Sets ModifiedUtc when IHasModifiedUtc.
- DeleteAsync: removes by reference/key.

## Set-Based Updates
ExecuteUpdateAsync applies the same change to every matching row in a single database round-trip, returning the number of rows affected:
```csharp
var revoked = await repo.ExecuteUpdateAsync(
    t => t.UserId == userId && t.RevokedUtc == null,
    s => s.SetProperty(t => t.RevokedUtc, utcNow));
```
Chain SetProperty for multiple columns, and pass an expression instead of a value to compute it per row server-side:
```csharp
await repo.ExecuteUpdateAsync(
    e => e.IsActive,
    s => s
        .SetProperty(e => e.Attempts, e => e.Attempts + 1)
        .SetProperty(e => e.ModifiedUtc, utcNow));
```
This bypasses the change tracker, so IHasModifiedUtc is **not** applied automatically; set it explicitly when needed. Rows are updated in place, so no entity is loaded or returned. Inside an active unit of work it runs at once within the unit of work's transaction, like every other write, so RollbackAsync undoes it and CommitAsync keeps it. The EF in-memory provider cannot run set-based updates; test them against SQLite in memory or a mock repo.

IUpdateSetters is declared by this library rather than surfacing EF's own setter type. EF has revised that type once already, and passing it through IRepo would make every such revision a breaking change for consumers.

## Set-Based Deletes
ExecuteDeleteAsync removes every matching row in a single database round-trip, returning the number of rows deleted:
```csharp
var purged = await repo.ExecuteDeleteAsync(t => t.ExpiresUtc < utcNow);
```
This runs straight against the database and bypasses the change tracker. Entities already loaded into the context stay tracked after their rows are gone, so don't save them again afterwards. Related rows follow the database's cascade rules, not EF's client side cascade, because nothing is loaded for EF to cascade over. Inside an active unit of work it runs at once within the unit of work's transaction, like every other write, so RollbackAsync restores the rows and CommitAsync keeps them deleted. As with updates, the EF in-memory provider cannot run set-based deletes; test them against SQLite in memory or a mock repo.

## Unit of Work Integration
See [Unit of Work](unit-of-work.md). In short:
- BeginAsync starts the unit of work; each write after it saves at once inside a transaction on its context.
- CommitAsync commits the transactions of all enlisted contexts.
- RollbackAsync rolls back transactions where present and clears ChangeTrackers.

## When to Subclass
Stick with the provided repos unless you need:
- Domain-specific query helpers
- Performance tweaks (compiled queries, projections)
- Raw SQL / stored procedures behind a safe API
- Cross-cutting behavior (soft delete, multi-tenancy, policies)

For tests that shouldn’t depend on EF Core semantics, use mock providers.
