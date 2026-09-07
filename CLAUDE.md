# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project Overview

Repository/Unit-of-Work layer over EF Core, plus an in-memory `MockRepo` used by consumers for
fast unit tests. Consumed by Corely.IAM.

## Read this before questioning the architecture

**[DESIGN-RATIONALE.md](DESIGN-RATIONALE.md)** records why a repository layer over EF Core exists
here, with Microsoft's own guidance and the cost it carries.

Do not re-litigate "is a repository over EF worth it" from first principles or from general
received wisdom about the pattern. That file already answers it, cites sources, states the cost
honestly, and lists the specific conditions that would justify changing course. If none of those
conditions hold, the answer stands.

This matters because the received wisdom ("DbContext is already a Unit of Work, don't wrap it")
does not apply to a codebase that needs substitutable data access for testing — and Microsoft's
testing documentation explicitly recommends the opposite for that case.

## Build and test

```powershell
.\RebuildAndTest.ps1
```

Tests run on xunit.v3 / Microsoft.Testing.Platform. `global.json` opts `dotnet test` into MTP mode;
without it the build fails on the .NET 10 SDK. Projects and solutions are named with `--project` /
`--solution` rather than positionally.

```powershell
dotnet test --solution Corely.DataAccess.sln
dotnet test --project Corely.DataAccess.UnitTests
```

## Conventions

Line endings are LF, enforced by `.gitattributes`. Formatting is CSharpier, enforced on build; the
pinned version lives in `.config/dotnet-tools.json`.

Keep EF-specific types out of `IRepo` / `IReadonlyRepo` signatures where practical. Every EF type
exposed there becomes a breaking change for consumers when EF revises it — that is the concrete
maintenance cost described in DESIGN-RATIONALE.md, and it is worth actively minimizing.

## Comments

Comments explain **why**, not what. The code says what it does; a comment that restates it is a
maintenance item that will drift out of date and mislead someone later.

Write one when the reason is not visible from the code:

- A non-obvious domain rule or constraint
- Why this approach was chosen over an obvious alternative
- A gotcha that would look like a bug to someone cleaning up

Do not write one for:

- What the next line does
- Restating a method or variable name in prose
- Narrating a sequence of steps that reads fine already

Prefer fixing the name over adding the comment. Keep them short - if a comment needs a paragraph,
it usually belongs in `Docs/` or a plan, not above the line.

```csharp
// BAD - restates the code
// Create the user
await CreateUserAsync(request);

// GOOD - the reason is not in the code
// Wildcard permission - Guid.Empty grants access to all resources of this type
if (permission.ResourceId == Guid.Empty) return true;
```

## Documentation

`Docs/` describes **how the current version works**. Nothing else.

- **No version numbers of this library.** No "since 2.1", "fixed in 3.0.2", "1.x did X". A reader on
  an older version is served by that version's docs. Migration guides are the sole exception and
  live at the repository root, not in `Docs/`.
- **No references to `Plans/`.** Plans are working material. Never link to one from documentation and
  never cite one as the reason something is the way it is.
- **Match the house style.** Terse and code-forward: a short orienting paragraph, then examples.
  Not an essay with nested headings. Read the neighbouring files in `Docs/` before adding one.
- **Legacy identifiers may be named, versions may not.** "The legacy name `X` stays registered as an
  alias" is fine; "the 1.x name `X`" is not.

The full guide is `DOCUMENTATION-STYLE.md` in the Corely.IAM repository.
