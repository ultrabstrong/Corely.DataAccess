# Repositories

> For concrete examples of registration and usage (CRUD + querying), inspect `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` where both simple and generic registrations are shown.

Generic repository abstractions simplify common EF Core data access patterns while remaining extensible.

## Interfaces
```csharp
public interface IReadonlyRepo<TEntity>
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity,bool>> query, ...);
    Task<bool> AnyAsync(Expression<Func<TEntity,bool>> query);
    Task<int> CountAsync(Expression<Func<TEntity,bool>>? query = null);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity,bool>>? query = null, ...);
}

public interface IRepo<TEntity> : IReadonlyRepo<TEntity>
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);
    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}
```

## Base Implementations
| Class | Purpose |
|-------|---------|
| EFReadonlyRepo<TEntity> | Query only operations (Get, Any, Count, List with include & order) |
| EFRepo<TEntity> | Adds Create / Update / Delete, ModifiedUtc management, UoW-aware deferred persistence |

## Registration Patterns
Open generics:
```csharp
services.AddScoped(typeof(IReadonlyRepo<>), typeof(EFReadonlyRepo<>));
services.AddScoped(typeof(IRepo<>), typeof(EFRepo<>));
```
Context-specific wrappers (Demo):
```csharp
public sealed class DemoRepo<T> : EFRepo<T> where T : class
{
    public DemoRepo(ILogger<EFRepo<T>> logger, DemoDbContext ctx) : base(logger, ctx) {}
}
```

## Query Customization
Each method supports:
- include: `Func<IQueryable<TEntity>, IQueryable<TEntity>>`
- orderBy: `Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>`
- predicate (for Get / Any / Count / List)

Example:
```csharp
var recent = await repo.ListAsync(
    e => e.IsActive,
    orderBy: q => q.OrderByDescending(x => x.CreatedUtc),
    include: q => q.Include(x => x.Children));

var activeCount = await repo.CountAsync(e => e.IsActive);
```

## Create Semantics
- Single entity: `await repo.CreateAsync(entity);`
- Batch: `await repo.CreateAsync(entities); // IEnumerable<TEntity>`
- If inside an active Unit of Work (`EFUoWProvider.BeginAsync()` called), changes are staged and flushed on `CommitAsync()`; otherwise `SaveChangesAsync` is called immediately (only if there are tracked changes).

## Update Semantics
`UpdateAsync(entity)`
- If an entity with the same primary key is already tracked, its values are updated via `Entry(tracked).CurrentValues.SetValues(entity)`.
- If not tracked, the entity is attached and marked `Modified`.
- Composite keys are supported (resolved from EF Core model metadata).
- If the entity implements `IHasModifiedUtc`, that timestamp is automatically updated.
- Within an active Unit of Work, the update is deferred until commit.

## Delete Semantics
`DeleteAsync(entity)`
- Removes the entity (by reference / key resolution by EF). Within a Unit of Work, deletion is deferred until commit.

## Unit of Work Integration
(See [Unit of Work](unit-of-work.md) for provider details and guidance.)
```csharp
await uow.BeginAsync();
await repo.CreateAsync(newEntity);
await repo.UpdateAsync(existingEntity);
await repo.DeleteAsync(toRemove);
await uow.CommitAsync(); // single SaveChanges + optional transaction commit
```
Rollback scenario (no transaction provider, e.g. InMemory): pending tracked changes are cleared on `RollbackAsync()`.

## When to Subclass Base Repo Implementations
Do NOT subclass if the base implementations already cover your needs (generic CRUD + deferred persistence in a UoW).

Subclass only when you need one of:
- Domain-specific query helpers (aggregate common query shapes)
- Performance optimizations (compiled queries, projection shortcuts, caching hooks)
- Raw SQL / stored procedure encapsulation behind a safe API
- Cross-cutting behaviors (soft delete, multi-tenancy, row-level security filters)
- Extended auditing or policy enforcement before/after operations
- Coordination across multiple DbContexts / databases behind a single facade

If only one feature is needed occasionally, consider extension methods or a service layer first—subclassing should add clear, reusable value.
