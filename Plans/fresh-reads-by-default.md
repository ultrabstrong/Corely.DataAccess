# Fresh reads by default

The owner, 2026-11-04, after a stale read surfaced in two apps: a repository read should return what is
in the database, not EF's remembered copy of it.

## The problem

1. Every repository read (`GetAsync`, `ListAsync`, `QueryAsync`, `EvaluateAsync`) is tracked. When the
   `DbContext` already tracks a row, EF returns the remembered copy and ignores what the query found.
2. A scope that outlives one operation therefore keeps serving stale rows. In Blazor Server a page's
   scope lives as long as the page, so:
   - DocsToData's job page kept showing the status it first loaded while the Functions host changed it
     (worked around with `AsNoTracking()` at each call site in `DocumentJobService`);
   - Corely.Billing.Web's grant list kept showing a cancelled request as pending after
     `ExecuteUpdateAsync` decided it directly in SQL.
3. The writes already behave as if nothing is remembered between calls: each one saves at once, and
   `UpdateAsync` accepts an entity it has never seen. Only the reads are stateful, and callers cannot
   see that from the interface.

## The shape

1. **Outside a unit of work, reads do not track.** Every read path, including the queryables handed
   to `QueryAsync` and `EvaluateAsync`, is no-tracking. What a caller gets is the database now.
2. **Outside a unit of work, writes leave nothing tracked.** After each save the tracker is cleared.
3. **Inside a unit of work, tracking is on for that operation.** Reads track, writes save, and commit
   or rollback clears the tracker. This is where an operation that changes a set of related rows
   together (a user and its roles) belongs.
4. **A tracked read for one operation without a transaction:** `IRepo<T>.GetForUpdateAsync`, returning
   a tracked entity whose changes `UpdateAsync` then saves column by column, and which is cleared
   after that save.
5. **No app-wide switch.** Whether reads are fresh never depends on configuration, so Corely.IAM and
   Corely.Billing behave the same in every host they run in.

## Audit first (2026-11-04 counts)

1. **Relationship changes saved through the tracker,** about 25 sites, all in Corely.IAM:
   `UserProcessor`, `GroupProcessor`, `RoleProcessor`, `PermissionProcessor`, `ApiKeyProcessor` and
   `AccountProcessor` add to or clear `Roles`, `Groups`, `Users`, `Permissions`, `ApiKeys` and
   `Accounts` on a loaded entity, then call `UpdateAsync` or delete. Each moves to
   `GetForUpdateAsync` or a unit of work, or the join rows silently stop saving.
2. **Read, change, update outside a unit of work,** about 90 `UpdateAsync` calls (Corely.IAM 59,
   Corely.Billing 9, DocsToData 22) against 9 units of work. Untracked, `UpdateAsync` writes every
   column. Each site moves to `GetForUpdateAsync`, so only changed columns are written, and any column
   two writers contend for (a job's status against a cancel request) is listed and given a direct
   update or a concurrency token.
3. **Saves that happen only because a later write saves the whole context:** an entity changed with
   no `UpdateAsync` of its own. Found by the test suites once the tracker is cleared, and fixed by
   calling `UpdateAsync`.

## Order

1. Corely.DataAccess: the shape above, with tests that a read after a change from another scope or a
   direct update is fresh, that a unit of work still saves a graph, and that the tracker is empty
   after a write and after commit. Major version.
2. Corely.IAM, then Corely.Billing: take it, work through the audit, full sequenced test passes
   including the provider matrices, release.
3. DocsToData: take the packages, remove the `AsNoTracking()` workarounds in `DocumentJobService`,
   check the job page and the grant list refresh on their own, full sequenced pass.
