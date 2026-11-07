# Fresh reads in long-lived scopes

**Status: done, no release.** No code changed; `Docs/repositories.md` gained "Scopes and Fresh
Reads". Corely.DataAccess stays at 3.2.1. A repository read in a long-lived scope can return a copy
remembered from earlier in that scope. The cause is the scope's lifetime, which the host controls, and
the fix belongs there. This file records why, so the question does not have to be worked out again.

## The problem

1. Every repository read is tracked. When the `DbContext` already tracks a row, EF returns the
   remembered instance and ignores what the query found. EF documents this as identity resolution: if
   the data changed in the database, "this will not be reflected in the results of the query."
2. In most hosts a scope lasts one request, so nothing is remembered for long. A Blazor Server
   component can keep a scope for as long as it is on screen, and every read after the first can be
   stale. Seen in two places:
   - a job page refreshing on a timer kept showing the status it first loaded while a background
     worker changed it;
   - a grant list reloaded after a cancel kept showing the request as pending, because the cancel was
     written with `ExecuteUpdateAsync` and the tracked copy was never updated.

## The rule this library follows

**One DI scope is one unit of work.** Within a scope, a read can return what that scope already
loaded, and changes to loaded entities are saved by the repository's writes. For data as the database
holds it now, use a new scope.

This passes the test that matters for a library meant to work with any LINQ ORM: it makes sense
without knowing EF. NHibernate's `ISession` behaves the same way and its standing advice is a session
per request or per unit of work; an ORM with no identity map is simply always fresh. Scoping is
ordinary DI, not an EF workaround.

## What was considered and rejected

Each of these was worked through before the cause was traced to scope lifetime.

| Option | Why not |
|--------|---------|
| No tracking outside a unit of work, plus `IRepo<T>.GetForUpdateAsync` for a tracked read | The caller has to know whether a read is for an update. It is EF's change tracker surfacing in the interface, a rule that makes no sense to someone using another ORM and is easy to forget |
| No tracking at all: a read is a snapshot, `UpdateAsync` writes the given entity's columns, relationships are written as rows of an explicit join entity | Breaks "read user, read role, add the role to the user, update the user", which works today because both stay tracked. Fixing it forces every many-to-many relationship onto an explicit join entity and every caller onto a new way of linking, which again exposes EF's mechanics to consumers |
| Keep tracking, and refresh in place any entity a read returns that was already tracked and unchanged | EF Core has no such feature, and building it is the work EF's own team has declined twice (below). Refreshing columns is simple; refreshing loaded collections, join entries and projections is where it goes wrong |
| A `DbContext` per repository call (`IDbContextFactory`) | Same break as no tracking: entities read in one call are untracked by the next, so the link pattern above stops saving |
| A configuration switch | Behaviour would differ per host, and the libraries above this one would have to cope with both |

### EF Core cannot refresh tracked entities from a query

- [dotnet/efcore#16491](https://github.com/dotnet/efcore/issues/16491), "Ability to refresh a context
  from the database", open since 2019, labelled for EF6 parity, in the Backlog milestone. EF6 had it as
  `MergeOption.OverwriteChanges`.
- A contributor implemented it twice. [#36556](https://github.com/dotnet/efcore/pull/36556) and
  [#37812](https://github.com/dotnet/efcore/pull/37812) were both closed unmerged in 2026. The EF team
  said the second had "multiple fundamental issues" and that the feature is "a big/complex one that's
  not suitable for external contributions."
- EF's documented answers are `EntityEntry.Reload` or `GetDatabaseValues` for one entity at a time,
  and, in general, "use a new DbContext instance for each unit-of-work"
  ([identity resolution](https://learn.microsoft.com/ef/core/change-tracking/identity-resolution#identity-resolution-and-queries)).

Reimplementing that inside a library whose point is to stay stable would be the riskiest change it
could make, for a problem the host can avoid.

## What this library does

1. `Docs/repositories.md` states the rule above: one scope is one unit of work, a read can return what
   the scope already loaded, fresh data means a new scope, and `AsNoTracking()` at a call site is not
   the fix.
2. Nothing else. Clearing the change tracker on `CommitAsync`, as `RollbackAsync` does, was
   considered and dropped: with a scope per operation the scope is disposed right after its commit,
   so there is nothing left to clear.

## What the hosts do

Microsoft's guidance for EF Core in Blazor Server
([Blazor with EF Core](https://learn.microsoft.com/aspnet/core/blazor/blazor-ef-core)) is, in order:
use a context per operation; scope a context to a component's lifetime only "for longer-lived
operations that take advantage of EF Core's change tracking."

### Already done, and why it does not cover this

Corely.IAM's `Plans/Completed/blazor-owned-scopes.md` moved every Blazor component in Corely.IAM.Web,
Corely.Billing.Web and DocsToData onto `OwningComponentBase`. That fixed what it set out to fix:
components loading in parallel on one shared context ("A second operation was started on this context
instance"). Each component now has its own scope, created with it and disposed with it.

That is Microsoft's second option, a context for the component's lifetime. It keeps a context as long
as the component is on screen, so it is exactly the lifetime that serves stale rows:

- `WorkflowJob` (DocsToData) reads the job through `ScopedServices` on a five second timer, in the same
  scope for as long as the page is open.
- `GrantList` (Corely.Billing.Web) cancels a request with `ExecuteUpdateAsync`, then reloads through the
  same component scope, and gets the tracked request back still pending.

DocsToData's `DocumentJobService` works around the first with `AsNoTracking()` on ten queries. That
dodges how the repositories behave at one call site rather than fixing the scope, and it leaves every
other component with the same exposure.

### What needs to change

**A scope per operation, not per component.** Each operation a component runs (a load, a timer
tick, an action and the reload after it) creates a scope, signs it in, resolves its services from it,
runs, and disposes it. Nothing is remembered between operations, so every read is fresh, and parallel
components still never share a context, so the concurrency fix is kept.

The exact work, component by component, is phase 2 of Corely.IAM's
`Plans/InProgress/blazor-owned-scopes.md`. In short: IAM.Web and Billing.Web gain an operation scope
service and base classes built on it in place of `OwningComponentBase`; every component resolves its
services inside an operation; DocsToData drops its `AsNoTracking()` calls. Two findings from working
it out:

- **Edits across operations need nothing.** Components hold models, never entities, and every update
  service takes a request model and does its own read and write inside one call.
- **A sign-in per operation**, about 30 ms as IAM measured it, and a page open past its token's
  lifetime now reloads on its next action instead of running on an expired token.

## As built

Only docs. Besides the new section, `Docs/index.md`, `Docs/repositories.md` and
`Docs/unit-of-work.md` said writes inside a unit of work are deferred until commit. They are not: each
write saves at once inside the unit of work's transaction. Those lines now say so, along with what
`UpdateAsync` writes and what rollback cannot undo on the in-memory provider and the mock. The host
work is tracked in Corely.IAM's plan, not here, and does not hold this one.

## Revisit when

- EF Core ships a refresh feature for tracked queries (dotnet/efcore#16491). The library could then
  make reads fresh within a scope without consumers changing anything.
- A host appears that cannot control its scope lifetime at all. None of the known hosts is one.
