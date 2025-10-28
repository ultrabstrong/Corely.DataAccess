# Copilot Instructions (C#/.NET)

Purpose: Generate code that fits this repository’s patterns and CI expectations with minimal noise and modern, idiomatic C#.

## Source of Truth

- Infer target frameworks, language version, nullable, analyzers, and tooling from:
  - Nearest .csproj
  - Directory.Build.props/targets
  - global.json, NuGet lockfiles
- Mirror established conventions (DI, logging, testing, analyzers, .editorconfig).

## Defaults

- If unspecified, assume current LTS (.NET 8) and modern C# features supported by the repo.
- Use built-in platform features before adding dependencies.
- Respect analyzers and fix warnings that CI treats as errors.

## Libraries and Frameworks

- Prefer versions declared in project files and lockfiles.
- Use ASP.NET Core idioms when applicable (Minimal APIs, built-in DI, logging abstractions, Options).
- Prefer standard primitives:
  - Span/Memory where appropriate
  - TimeProvider over DateTime.UtcNow (when .NET 8+)
  - HttpClientFactory for HTTP
- Do not add new dependencies unless necessary and consistent with repo policy.

## C# Coding Standards (concise)

- Clarity first: small types, expressive names, pure/narrow functions; minimize comments—only for intent, invariants, or non-obvious behavior.
- Constructors:
  - Prefer primary constructors when supported; otherwise standard constructors with readonly fields/properties.
  - Validate public inputs early (ArgumentNullException.ThrowIfNull(arg)).
  - Prefer immutable or init-only state for constructor-defined data.
- Collections and LINQ:
  - Use collection expressions and initializers when supported and clear.
  - Use LINQ for straightforward transforms; switch to explicit loops for performance or clarity.
- Modern features:
  - File-scoped namespaces.
  - Usings at top; prefer using directives over fully qualified names; use aliases only for ambiguity.
  - Nullability enabled; use ? appropriately.
  - var when type is evident or non-essential to understanding.
  - Expression-bodied members for simple cases.
  - Pattern matching and switch expressions where they improve readability.
- Async:
  - Propagate async/await; avoid .Result/.Wait().
  - Accept CancellationToken ct as last parameter.
  - Suffix async methods with Async.
- Exceptions:
  - Throw specific exceptions.
  - Don’t swallow or use exceptions for control flow.
- Immutability:
  - Prefer record/record struct or init-only DTOs; use readonly where applicable.
- Dependency Injection:
  - Constructor injection; avoid service locator in app code.
- Logging:
  - Use ILogger<T> with structured placeholders (e.g., {OrderId}).

## Naming and Layout

- One top-level type per file; keep files focused.
- PascalCase for types/members; camelCase for locals/parameters; _camelCase for private fields; SCREAMING_SNAKE_CASE for constants.
- Interfaces start with I; async methods end with Async.
- Place InternalsVisibleTo in the project (.csproj) instead of AssemblyInfo attributes.

## Testing and Tooling

- Match repo’s test framework and assertion library.
- Clear Arrange/Act/Assert; deterministic and isolated tests.
- Prefer TimeProvider for time; avoid hard-coded clocks.
- Respect .editorconfig and analyzers; ensure formatting and code style pass CI.

## How Copilot Should Respond (C# focus)

- Use versions and patterns inferred from project files; otherwise .NET 8 + modern C#.
- Provide complete, compilable code with required usings and project-compatible APIs.
- Apply the standards above:
  - Use primary constructors and collection expressions when supported; fall back gracefully if not.
  - Initialize empty collections with [] when the type is evident.
  - Prefer using directives; add file/global usings or aliases if needed.
- Mirror repository DI, logging, options/configuration, and testing patterns.
- Ask concise clarifying questions when requirements are ambiguous.

## Compatibility fallback (when C# 12 features aren’t supported)

- Replace primary constructors with standard constructors and readonly fields/properties.
- Replace collection expressions with initializers or explicit construction plus Add calls.