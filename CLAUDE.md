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
