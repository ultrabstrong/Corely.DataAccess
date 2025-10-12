# Unit of Work

A lightweight boundary to group multiple repository writes into a single logical operation. While a unit of work (UoW) is active, repositories defer persistence; on commit, changes are saved together. On rollback, pending changes are discarded.

## Interface
```csharp
public interface IUnitOfWorkProvider
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

## How to Use

Basic pattern:
```csharp
var uow = sp.GetRequiredService<IUnitOfWorkProvider>();
await uow.BeginAsync();
try
{
    var repo = sp.GetRequiredService<IRepo<MyEntity>>();
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

## Registration
Register repositories and the UoW with the provided helper:
```csharp
services.RegisterEntityFrameworkReposAndUoW();
```
You also need to register your DbContexts and one provider configuration (see [Configurations](configurations.md)).

For provider‑free unit tests:
```csharp
services.RegisterMockReposAndUoW();
```

## Notes
- Participation and DI scopes: A repository participates when it’s resolved from the same DI scope as the active UoW instance. Repositories resolved from a different DI scope are not enlisted automatically.
- You can continue to use repositories after `CommitAsync()`; subsequent calls are no longer part of a UoW unless `BeginAsync()` is called again.
- For EF-based setups, the appropriate DbContext for each entity is resolved at runtime. If none or multiple contexts match, an error is thrown.