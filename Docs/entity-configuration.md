# Entity Configuration & Helpers

Entity configuration base classes reduce repetition for common persistence concerns (table naming, keys, audit columns).

Note: To avoid repeating provider setup and configuration discovery in each DbContext, you can inherit from EFConfiguredDbContext, which wires IEFConfiguration and automatically applies EntityConfigurationBase<> types from the context assembly.

## Base Concepts

### EntityConfigurationBase<TEntity>
Provides (for any class entity):
- Table name normalization from class name
- CreatedUtc & ModifiedUtc column mapping when marker interfaces implemented
- Provider-agnostic column type + default value via injected IEFDbTypes
- Hook (ConfigureInternal) for custom property/index/relationship config

Intentionally does NOT configure a primary key.

### EntityConfigurationBase<TEntity, TKey>
Adds to the above:
- Primary key configuration via ConfigureIdPk<TEntity,TKey>() (Key + ValueGeneratedOnAdd)

Use this when the entity implements IHasIdPk<TKey>.

## Table Naming Convention
- Removes trailing "Entity"
- Appends "s" if the result does not already end with "s"

Example: DemoEntity -> Demos

## Audit Columns
Markers:
- IHasCreatedUtc -> CreatedUtc non-null with provider default value
- IHasModifiedUtc -> ModifiedUtc nullable; set by repo update logic

## Example Configuration
```csharp
internal sealed class DemoEntityConfiguration : EntityConfigurationBase<DemoEntity, int>
{
    public DemoEntityConfiguration(IEFDbTypes db) : base(db) {}
    protected override void ConfigureInternal(EntityTypeBuilder<DemoEntity> b)
    {
        b.Property(e => e.Name).HasMaxLength(128).IsRequired();
    }
}
```

## Automatic Discovery
The demo DbContexts reflect the assembly to locate derived configurations; alternatively register explicitly:
```csharp
modelBuilder.ApplyConfiguration(new DemoEntityConfiguration(efConfig.GetDbTypes()));
```

## Tips
- Keep configuration classes small.
- Reuse constants for max lengths or other constraints.
- Put advanced mapping (owned types, value converters, query filters) inside ConfigureInternal.
