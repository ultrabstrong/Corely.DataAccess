# Mock Repositories

Lightweight in-memory implementations are provided for fast unit tests and demos without a real database:

| Type | Implements | Purpose |
|------|------------|---------|
| `MockRepo<TEntity>` | `IRepo<TEntity>` | Full CRUD + query operations backed by an in-memory `List<TEntity>` |
| `MockReadonlyRepo<TEntity>` | `IReadonlyRepo<TEntity>` | Read-only wrapper reusing the same underlying list (share state with a `MockRepo<TEntity>`) |

## When To Use
- Unit tests that do not assert EF Core change tracking specifics
- Prototyping or console demos
- Verifying repository contract logic without a provider dependency

Avoid for: behavior relying on EF Core query translation, relational constraints, transactions, or concurrency.

## Behavior Notes
- All methods are `virtual` enabling overriding / spying.
- `Include` and `OrderBy` delegates are executed against LINQ-to-Objects (`IQueryable`).
- `CountAsync` supports optional predicate (mirrors EF variant) and executes synchronously then wraps result in a completed task.
- `UpdateAsync` attempts to replace the exact reference only (no key resolution). Designed for simple scenarios.
- `IHasModifiedUtc` entities have `ModifiedUtc` set during `UpdateAsync` (mirrors EF implementation behavior).

## Basic Usage
```csharp
var mockRepo = new MockRepo<MyEntity>();
await mockRepo.CreateAsync(new MyEntity { Id = 1, Name = "Alpha" });
await mockRepo.CreateAsync(new MyEntity { Id = 2, Name = "Beta" });

bool anyBeta = await mockRepo.AnyAsync(e => e.Name == "Beta"); // true
int total = await mockRepo.CountAsync();                        // 2
int filtered = await mockRepo.CountAsync(e => e.Name.Contains("a"));
var first = await mockRepo.GetAsync(e => e.Id == 1);
var ordered = await mockRepo.ListAsync(orderBy: q => q.OrderBy(e => e.Name));
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
    public async Task Service_Uses_CountAsync()
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
| LINQ translation errors | Not surfaced | Thrown at runtime/execution |
| Change tracking graph resolution | Reference equality only | Key + tracking graph resolution |
| Transactions | Not supported | Supported via DbContext/Database |
| Query performance | In-memory list | Provider dependent |

If your test needs to validate EF translation or relational constraints, prefer the EF InMemory or a real provider with a test database.

## Summary
Use the mock repositories for fast, deterministic tests focusing on business logic around repository contracts—not for validating EF Core provider behavior.
