# Entity Configuration & Helpers

> For end‑to‑end examples (automatic discovery + registration), see the `Corely.DataAccess.Demo` and `Corely.DataAccess.DemoApp` projects. They show how configurations are discovered and applied at runtime.

Entity configuration base classes reduce repetition for common persistence concerns (table naming, keys, audit columns).

## Base Concepts

### EntityConfigurationBase<TEntity>
Provides (for any class entity):
- Table name normalization from class name
- CreatedUtc & ModifiedUtc column mapping when marker interfaces implemented
- Provider-agnostic column type + default value via injected `IEFDbTypes`
- Hook (`ConfigureInternal`) for custom property, index, relationship, owned-type, converter, value generation configuration

It intentionally does NOT configure a primary key. Use this when:
- The entity has a composite key configured manually
- The entity is keyless / a view / a query type
- You want to control key setup yourself in `ConfigureInternal`

### EntityConfigurationBase<TEntity, TKey>
Provides everything listed above for `EntityConfigurationBase<TEntity>` PLUS:
- Primary key configuration via `ConfigureIdPk<TEntity,TKey>()` (Key + ValueGeneratedOnAdd)

Use this when the entity implements `IHasIdPk<TKey>` and you want standard auto-increment / identity semantics.

You generally do NOT need to look at internal implementation; treat both as orchestrators of helper extension methods.

## Table Naming Convention
`ConfigureTable()` (details repeated for clarity):
- Removes trailing `Entity`
- Appends `s` if the result does not already end with `s`

Example: `DemoEntity` -> `Demos` (after suffix removal + pluralization)

## Audit Columns
Markers:
- `IHasCreatedUtc` -> `CreatedUtc` non-null with provider default value (CURRENT_TIMESTAMP / UTC_TIMESTAMP / etc.)
- `IHasModifiedUtc` -> `ModifiedUtc` nullable, no default; updated manually (e.g. in repository update logic)

## Example Configuration
```csharp
internal sealed class DemoEntityConfiguration : EntityConfigurationBase<DemoEntity, int>
{
    public DemoEntityConfiguration(IEFDbTypes db) : base(db) {}
    protected override void ConfigureInternal(EntityTypeBuilder<DemoEntity> b)
    {
        b.Property(e => e.Name)
            .HasMaxLength(128)
            .IsRequired();
    }
}
```

## Automatic Discovery
The demo `DemoDbContext` reflects its assembly to locate derived configurations; alternatively you can register explicitly:
```csharp
modelBuilder.ApplyConfiguration(new DemoEntityConfiguration(efConfig.GetDbTypes()));
```

## When to Create / Override a Configuration
Create or modify a configuration when you need:
- Custom table / schema / naming logic beyond defaults
- Additional indexes or unique constraints
- Relationship mapping (HasMany / OwnsOne / Owned types)
- Property conversions, precision/scale, or value converters
- Shadow properties or global query filters (applied inside `ConfigureInternal`)

## Tips
- Keep configuration classes small; push computed logic to domain services.
- Reuse constants (e.g. max lengths) from a shared static class to avoid magic numbers.
- Prefer indexes defined here rather than in ad-hoc migrations for clarity.
