# Unit of Work

> For a working example (registration + Begin/Commit pattern) see the demo projects: `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp`.

Provides optional transaction boundary abstraction across repository operations.

## Interface
```csharp
public interface IUnitOfWorkProvider
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

## EFUoWProvider
Wraps a DbContext transaction. Skips starting a transaction for InMemory provider.

Lifecycle:
```csharp
await uow.BeginAsync();
try
{
    // All three operations succeed or fail together
    await repo.CreateAsync(entity);
    await repo2.UpdateAsync(entity2, x => x.Id == entity2.Id);
    await repo3.DeleteAsync(entity3);
    // Commit if all succeeded
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync();
    throw;
}
```

## Registration
```csharp
services.AddScoped<IUnitOfWorkProvider, EFUoWProvider>();
```
Or a context-specific wrapper (Demo):
```csharp
public sealed class DemoUoWProvider : EFUoWProvider
{
    public DemoUoWProvider(DemoDbContext ctx) : base(ctx) {}
}
```

## Guidance
- Use only when multiple repository operations must succeed / fail together.
- For single Create/Update/Delete calls, ambient SaveChanges in repository is fine.
- Keep transaction scope short to reduce locking and improve concurrency.
