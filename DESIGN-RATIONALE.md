# Why Corely.DataAccess exists

This file answers one recurring question: **is a repository layer over EF Core worth maintaining?**

It gets asked every time EF ships a major version and something here breaks. The answer has been
"yes" each time, and the reasoning is below so it does not have to be re-derived from scratch.

## Short version

Microsoft recommends exactly this pattern for applications that want to test without a database.
It is not a workaround or a legacy habit — it is the documented approach, and the maintenance cost
is documented alongside it.

> If you've decided to use a test double, we recommend implementing the repository pattern, which
> allows you to stub or mock out your data access layer above EF Core, rather than using a fake EF
> Core provider (Sqlite/in-memory) or by mocking `DbSet`.
>
> — [Choosing a testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy#summary)

## The three objections, and the answers

### "It's a leaky abstraction — it exposes EF types"

Mostly it does not. `Expression<Func<T, bool>>`, `IQueryable<T>` and
`Func<IQueryable<T>, IOrderedQueryable<T>>` are `System.Linq`, not EF. Any LINQ-enabled ORM
satisfies those signatures.

The genuinely EF-typed surface has historically been a single parameter: the `ExecuteUpdateAsync`
setters. That is worth keeping an eye on and worth wrapping, but it is one parameter on one method
— not evidence that the design is wrong.

### "Couldn't the EF in-memory provider replace MockRepo?"

No, and it is the option Microsoft discourages most strongly.

> Avoid the in-memory provider for testing purposes — this is discouraged and only supported for
> legacy applications.
>
> in-memory has all the disadvantages of SQLite, along with a few more — and offers no advantages
> in return.
>
> The in-memory provider has not been optimized for performance, and will generally work slower
> than SQLite in in-memory mode.

Concretely, for this codebase: the in-memory provider cannot execute `ExecuteUpdateAsync`, which
Corely.IAM uses for token revocation and password-recovery expiry. Those tests could not run at all.

### "Isn't IRepo redundant?"

No. `DbContext` and `DbSet<T>` are classes with no interface suitable for injection or
substitution. Any application that wants an injectable, substitutable data layer has to define that
interface itself. `IRepo<T>` is that interface — it is the thing you would write anyway, not an
extra layer on top of one you already had.

## What Microsoft's own comparison shows

| Feature | In-memory | SQLite in-memory | Mock DbContext | **Repository pattern** | Real database |
|---|---|---|---|---|---|
| Raw SQL | No | Depends | No | **Yes** | Yes |
| Transactions | No (ignored) | Yes | Yes | **Yes** | Yes |
| Provider-specific translations | No | No | No | **Yes** | Yes |
| Exact query behavior | Depends | Depends | Depends | **Yes** | Yes |
| LINQ anywhere in the app | Yes | Yes | Yes | **No\*** | Yes |

\* Testable queries must be encapsulated in repository methods. See the honest caveat below.

## What it costs

Microsoft states the cost plainly, and it is real:

> that approach has a significant cost in terms of implementation and maintenance.

For this codebase that cost is:

- Roughly 1,900 lines to maintain, about 550 of which is `MockRepo`.
- A hand-written re-implementation of EF semantics that can drift from real EF behavior. The
  integration tier exists to cover what the mock cannot model — translation, constraints, provider
  differences.
- Breakage on EF major versions when EF changes a type this interface exposes.

Pay it knowingly. It buys a unit suite that runs in seconds instead of minutes.

## The one honest caveat

`IReadonlyRepo.QueryAsync` and `EvaluateAsync` take `Func<IQueryable<T>, ...>` passthroughs.
Callers using those write LINQ that executes inside the repository, which `MockRepo` models only
approximately. That is the one place the abstraction genuinely thins, and it is the tradeoff for
not forcing every projection into a bespoke repository method.

Behavior that depends on real SQL translation belongs in the integration tier, not the mock.

## When to revisit this decision

Reopen the question if any of these become true — not merely because an upgrade was annoying:

- The unit suite stops being meaningfully faster than the integration suite. Speed is the main
  thing being bought; if it is gone, so is the justification.
- `MockRepo` drift starts producing false passes — unit tests green while integration tests catch
  the same logic failing.
- EF exposes a supported abstraction over `DbContext`/`DbSet` suitable for substitution, removing
  the reason to define one here.
- The consuming application stops needing fast isolated tests of data-dependent logic.

Absent those, the answer is the same as last time.

## Sources

- [Testing EF Core Applications](https://learn.microsoft.com/ef/core/testing/)
- [Choosing a testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy)
- [Testing without your production database system](https://learn.microsoft.com/ef/core/testing/testing-without-the-database)
