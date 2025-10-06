# Mock Repositories

Lightweight in-memory implementations are provided for fast unit tests and demos without a real database:

| Type | Implements | Purpose |
|------|------------|---------|
| `MockRepo<TEntity>` | `IRepo<TEntity>` | Full CRUD + query operations backed by an in-memory `List<TEntity>` |
| `MockReadonlyRepo<TEntity>` | `IReadonlyRepo<TEntity>` | Read-only wrapper reusing the same underlying list (share state with a `MockRepo<TEntity>`) |

## When To Use
- Unit tests that do not assert EF Core change tracking specifics
- Prototyping or console demos
- Verifying service / domain logic against repository contracts without a provider dependency

Avoid for: scenarios relying on EF Core query translation, relational constraints, concurrency tokens, or actual transaction semantics.

## Behavior Notes
- All methods are `virtual` enabling overriding / spying.
- `Include` and `OrderBy` delegates run against LINQ-to-Objects.
- `CountAsync` and `AnyAsync` compile the expression and evaluate in memory.
- `CreateAsync` auto-sets `CreatedUtc` if entity implements `IHasCreatedUtc` and value is default.
- `UpdateAsync` sets `ModifiedUtc` when implementing `IHasModifiedUtc` and now prefers key-based replacement when entity implements `IHasIdPk<TKey>`; falls back to reference equality if no key.
- `DeleteAsync` removes by key when available; otherwise by reference.

## Basic Usage
```csharp
var repo = new MockRepo<MyEntity>();
await repo.CreateAsync(new MyEntity { Id = 1, Name = "Alpha" });
await repo.CreateAsync(new [] { new MyEntity { Id = 2 }, new MyEntity { Id = 3 } });
var item = await repo.GetAsync(e => e.Id == 2);
await repo.UpdateAsync(new MyEntity { Id = 2, Name = "Updated" });
var list = await repo.ListAsync(); // contains updated entity
```

## Readonly Wrapper
```csharp
var fullRepo = new MockRepo<MyEntity>();
var readonlyRepo = new MockReadonlyRepo<MyEntity>(fullRepo); // shares same backing list
await fullRepo.CreateAsync(new MyEntity { Id = 10, Name = "Gamma" });
int count = await readonlyRepo.CountAsync(); // 1
```

## Test Example Pattern
```csharp
public class MyServiceTests
{
    private readonly MockRepo<MyEntity> _repo = new();

    [Fact]
    public async Task Service_Counts()
    {
        await _repo.CreateAsync(new MyEntity { Id = 1, Name = "A" });
        await _repo.CreateAsync(new MyEntity { Id = 2, Name = "B" });
        var active = await _repo.CountAsync(e => e.Id > 0);
        Assert.Equal(2, active);
    }
}
```

## Limitations vs EF
| Concern | Mock | EF Core |
|---------|------|---------|
| LINQ translation errors | Not surfaced (all in-memory) | Thrown at execution | 
| Change tracking resolution | Key or reference replacement only | Full key + relationship tracking | 
| Transactions | Not supported | Supported (DbContext / Database) | 
| Concurrency tokens / store gen | Ignored | Enforced / generated | 
| Query performance | In-memory list | Provider dependent | 

## Summary
Mock repos trade fidelity for speed and determinism. Use them to test business logic, not EF Core translation or relational behavior. Promote tests needing real provider semantics to an integration test layer.
