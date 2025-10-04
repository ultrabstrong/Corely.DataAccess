# Repositories

> For concrete examples of registration and usage (CRUD + querying), inspect `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` where both simple and generic registrations are shown.

Generic repository abstractions simplify common EF Core data access patterns while remaining extensible.

## Interfaces
```csharp
public interface IReadonlyRepo<TEntity>
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity,bool>> query, ...);
    Task<bool> AnyAsync(Expression<Func<TEntity,bool>> query);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity,bool>>? query = null, ...);
}

public interface IRepo<TEntity> : IReadonlyRepo<TEntity>
{
    Task<TEntity> CreateAsync(TEntity entity);
    Task CreateAsync(params TEntity[] entities);
    Task UpdateAsync(TEntity entity, Func<TEntity,bool> identityPredicate);
    Task DeleteAsync(TEntity entity);
}
```

## Base Implementations
| Class | Purpose |
|-------|---------|
| EFReadonlyRepo<TEntity> | Query only operations (Get, Any, List with include & order) |
| EFRepo<TEntity> | Adds Create / Update / Delete and ModifiedUtc management |

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
- predicate (for Get / Any / List)

Example:
```csharp
var recent = await repo.ListAsync(
    e => e.IsActive,
    orderBy: q => q.OrderByDescending(x => x.CreatedUtc),
    include: q => q.Include(x => x.Children));
```

## Update Semantics
`UpdateAsync(entity, predicate)`
- If entity tracked locally (matched via predicate) values are set via `Entry(existing).CurrentValues.SetValues(entity)`
- If not, entity is attached and marked Modified
- If entity implements `IHasModifiedUtc`, `ModifiedUtc` auto-updated

## When to Create a Custom Repo
- Add domain-specific query helpers aggregated from multiple generic queries
- Optimize frequently used patterns with compiled queries
- Encapsulate raw SQL when needed
