# ExecuteDeleteAsync

**Status: done, released as Corely.DataAccess 3.2.0.** Everything below shipped as planned. What the
work found beyond the plan is under "What was done".

`IRepo` can update every matching row in one round trip (`ExecuteUpdateAsync`) but can only delete
one entity at a time (`DeleteAsync(entity)`). Add the set based delete: the "DeleteWhere" half of the
old "support `UpdateWhere` and `DeleteWhere`" idea, whose update half shipped as
`ExecuteUpdateAsync`.

## Model after `ExecuteUpdateAsync`

Commits `4e7332e` (the method and its parity tests) and `c8ecb22` (its docs) are the template. Copy
their shape file for file:

| File | `ExecuteUpdateAsync` today | `ExecuteDeleteAsync` |
|---|---|---|
| `Corely.DataAccess/Interfaces/Repos/IRepo.cs` | `Task<int> ExecuteUpdateAsync(query, setProperties, ct)` | `Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> query, CancellationToken cancellationToken = default)`, placed after `DeleteAsync` |
| `Corely.DataAccess/EntityFramework/Repos/EFRepo.cs` | Null guards, register with the unit of work when active, `DbSet.Where(query).ExecuteUpdateAsync(...)` | Same guards and unit of work registration, then `DbSet.Where(query).ExecuteDeleteAsync(cancellationToken)` |
| `Corely.DataAccess/EntityFramework/Repos/EFRepoAdapter.cs` | One line forwarding to `Repo` | Same |
| `Corely.DataAccess/Mock/Repos/MockRepo.cs` | `virtual`; compiles the query, applies setters to each match, returns the count | `virtual`; compiles the query, removes every match from `Entities`, returns the count |
| `Corely.DataAccess.UnitTests/ExecuteUpdateParityTests.cs` | Runs each case against `EFRepo` on SQLite in memory and `MockRepo`, and asserts both end the same | New `ExecuteDeleteParityTests.cs` with the same fixture, seed and `BothRepos` loop |
| `Docs/repositories.md` | Signature in the interface listing, a section with examples | Signature in the listing, and a "Set-Based Deletes" section after "Set-Based Updates" with one example |
| `Docs/mock-repositories.md` | A line saying how the mock behaves | A line saying the mock removes every match and returns the count |

Delete is simpler than update: there are no setters, so nothing like `IUpdateSetters`,
`EFUpdateSetters` or `MockUpdateSetters` is needed.

The name follows the sibling and EF (`ExecuteDeleteAsync`), not the idea's working name `DeleteWhere`.

## Parity tests

1. Deletes only the matching rows and returns how many.
2. Returns zero and deletes nothing when no rows match.
3. A query matching every row empties the set.
4. A null query throws `ArgumentNullException` on both repos.

## Behaviour to document

1. Like EF, it runs straight against the database and bypasses change tracking: entities already
   loaded into the context stay tracked after their rows are gone. Say so in `Docs/repositories.md`,
   as the `ExecuteUpdateAsync` section does for `IHasModifiedUtc`.
2. Related rows follow the database's cascade rules, not EF's client side cascade. `MockRepo` has no
   relations and removes only the matching entities.

## Release

Adding a member to `IRepo` breaks anyone who implements it. The only implementations in the
`ultrabstrong` repositories are this library's own (`EFRepo`, `EFRepoAdapter`, `MockRepo`), so a
minor version bump fits, following the release steps in `CLAUDE.md`.

## Done when

1. `RebuildAndTest.ps1` passes with the new parity tests.
2. Docs updated as above, with no version numbers and no reference to this plan.
3. Released, or handed back ready to tag.

## What was done

1. `ExecuteDeleteAsync` was added to `IRepo`, `EFRepo`, `EFRepoAdapter` and `MockRepo` as the table
   lays out. The code had moved on since `4e7332e` (`IUpdateSetters`, a `TimeProvider` on `EFRepo`),
   so the current `ExecuteUpdateAsync` was the model rather than that commit's diff, and its comments
   were left out under the no comments rule.
2. `ExecuteDeleteParityTests` covers the four parity cases on both repos, plus four tests on `EFRepo`
   alone, since the mock has no unit of work or relations:
   1. a unit of work rollback restores the deleted rows;
   2. a commit keeps them deleted;
   3. an entity loaded before the delete stays tracked as `Unchanged` after its row is gone;
   4. deleting a principal that a dependent row still references fails on the database's foreign
      key, where `DeleteAsync` would have nulled the reference client side.

   Removing the unit of work registration from `EFRepo.ExecuteDeleteAsync` fails the rollback test.
3. The plan missed that the docs say repositories defer writes until `CommitAsync` while a unit of
   work is active. Set-based operations do not: they run at once inside the unit of work's
   transaction. `Docs/repositories.md` now says so in both "Set-Based Updates" and "Set-Based
   Deletes".
4. The EF in-memory provider cannot run `ExecuteDeleteAsync` or `ExecuteUpdateAsync`. Both sections
   now say so and point at SQLite in memory or `MockRepo`.
5. Claims 3 and 4 for `ExecuteUpdateAsync` were checked with a throwaway test that was not kept, so
   no permanent test covers an update under a rolled back unit of work.
6. `Docs/mock-repositories.md` gained the behaviour line and a row in the limitations table.
7. The release reasoning held: no `IRepo` implementation outside this library exists in the local
   `ultrabstrong` checkouts.
