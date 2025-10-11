# Unit of Work

A lightweight boundary to group multiple repository writes into a single logical operation. While a unit of work (UoW) is active, repositories defer persistence; on commit, changes are saved together. On rollback, pending changes are discarded.

## Interface
```csharp
public interface IUnitOfWorkProvider
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
    IRepo<TEntity> GetRepository<TEntity>() where TEntity : class;
}
```

## How to Use

Basic pattern:
```csharp
var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
try
{
    var repo = uow.GetRepository<MyEntity>();
    await repo.CreateAsync(new MyEntity { /* ... */ });
    await repo.UpdateAsync(existing);

    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

- Without an active UoW, repositories save immediately (typical for single operation calls).
- With an active UoW, repositories defer persistence until CommitAsync().

### Early vs Late Repository Resolution
Both are supported.

Early (resolve repo before Begin):
```csharp
var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
var repo = uow.GetRepository<MyEntity>();
await uow.BeginAsync();
await repo.CreateAsync(new MyEntity { /* ... */ });
await uow.CommitAsync();
```

Late (resolve repo after Begin):
```csharp
var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
var repo = uow.GetRepository<MyEntity>();
await repo.CreateAsync(new MyEntity { /* ... */ });
await uow.CommitAsync();
```

In both cases, the repository participates in the active UoW and its changes are included in the commit. Within the same DI scope, calling `GetRepository<T>()` enlists the scoped repository instance; any previously resolved references of that type in the same scope are enlisted as well. If the underlying provider supports transactions, the UoW will commit them together; otherwise it still batches SaveChanges.

## Registration
Register repositories and the UoW with the provided helper:
```csharp
services.RegisterEntityFrameworkReposAndUoW();
```
You also need to register your DbContexts and one provider configuration (see [Configurations](configurations.md)).

For provider?free unit tests:
```csharp
services.RegisterMockReposAndUoW();
```

## Notes
- Participation and DI scopes: A repository participates when the active UoW scope has been applied to that scoped repository instance. Calling `uow.GetRepository<TEntity>()` within the current DI scope enlists that scoped instance; any references to the same scoped instance are enlisted too. Repositories resolved from a different DI scope are not enlisted automatically.
- Recommendation: Inside a UoW, either resolve repositories via `GetRepository<T>()` or call it at least once per repo type you will use in the current DI scope before writing.
- You can continue to use repositories after `CommitAsync()`; subsequent calls are no longer part of a UoW unless `BeginAsync()` is called again.
- For EF-based setups, the appropriate DbContext for each entity is resolved at runtime. If none or multiple contexts match, an error is thrown.