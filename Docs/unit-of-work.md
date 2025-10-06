# Unit of Work

> See `Corely.DataAccess.Demo` / `Corely.DataAccess.DemoApp` for a runnable example.

Provides an optional boundary that (a) batches SaveChanges into a single flush and (b) wraps it in a transaction when the provider supports it.

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
- Starts a transaction for relational providers.
- Skips transaction for InMemory but still defers SaveChanges until Commit.
- Rollback clears tracked (unflushed) changes if no transaction was started.

Lifecycle (happy path + failure):
```csharp
await uow.BeginAsync();
try
{
    await repo.CreateAsync(e1);
    await repo.UpdateAsync(e2);
    await repo.DeleteAsync(e3);
    await uow.CommitAsync(); // single SaveChanges (+ COMMIT if tx)
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
Or subclass:
```csharp
public sealed class DemoUoWProvider(DemoDbContext ctx) : EFUoWProvider(ctx);
```

## When to Use a UoW
Use it when you need:
- Multiple writes must succeed/fail together.
- Batch several small changes (reduce round trips).
- A clear atomic boundary for invariants.

Skip it for:
- Single CRUD operations.
- Pure reads.
- Long-running or external-call heavy workflows (keep tx short).

## When to Subclass `EFUoWProvider`
Do NOT subclass if the base implementation already covers your needs (single DbContext + atomic multi-write + deferred SaveChanges).

Subclass only when you need one of:
- Post-commit side effects (domain/integration event publish, outbox dispatch, message enqueue)
- Cross-cutting telemetry & auditing (logs, metrics, traces, enriched audit fields) executed exactly once per commit
- Policy & security gates (tenant / permission validation, concurrency rules) at Begin or Commit
- Multi-store coordination (multiple DbContexts / heterogeneous stores under one logical boundary)
- Reliability wrappers (custom retry, deadlock/backoff policy around commit)
- Lifecycle hooks (pre-commit validation, pre-rollback cleanup, post-rollback compensations)

If only one behavior is needed occasionally, consider composition (call-site helper or decorator) before subclassing—the subclass should provide clear reusable value.