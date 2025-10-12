# Entity Configuration & Helpers

Entity configuration base classes reduce repetition for common persistence concerns (table naming, keys, audit columns).

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

### How CreatedUtc works (IHasCreatedUtc)
- Mapping
  - Column type and default SQL are provided by IEFDbTypes (per provider).
  - ValueGeneratedOnAdd is applied.
  - EF Core save behaviors are set to Ignore before and after save, so EF does not attempt to write or update CreatedUtc; the database sets it on insert and it remains immutable thereafter.
- Insert semantics
  - On relational providers (SQLite/MySQL/Postgres/etc.), the DB default (e.g., CURRENT_TIMESTAMP/UTC_TIMESTAMP) is used. EF marks the property as store-generated; the value is available on the tracked entity after SaveChanges.
  - On the InMemory provider, database defaults are not executed. CreatedUtc will remain default unless you set it in code (only relevant for unit tests that assert on CreatedUtc).
- Update semantics
  - EF will ignore application-side changes to CreatedUtc due to AfterSave Ignore; the column is not sent on UPDATE and will remain unchanged in the database.
- Customizing
  - If you need to set CreatedUtc manually on insert (instead of DB default), remove HasDefaultValueSql in your configuration and assign a value in code.
  - If you need to allow updates to CreatedUtc, change the AfterSave behavior to Save (not recommended for immutable create timestamps).

### How ModifiedUtc works (IHasModifiedUtc)
- Mapping
  - Column type is configured via IEFDbTypes. No default value is set; the property is nullable.
- Update semantics
  - The EF repository (EFRepo.UpdateAsync) sets ModifiedUtc = DateTime.UtcNow on updates for entities implementing IHasModifiedUtc. This happens when calling UpdateAsync, not at transaction commit.
  - If you need commit-time or database-side timestamps (e.g., triggers), disable the repo behavior and implement it via EF interceptors or database triggers.
- Insert semantics
  - ModifiedUtc is typically null on insert.
- Customizing
  - You can add your own conventions (e.g., global SaveChanges interceptor) to set ModifiedUtc for any tracked entity implementing the interface.

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
- For tests using the InMemory provider, set CreatedUtc in code if your assertions depend on it (DB defaults are not executed by InMemory).
