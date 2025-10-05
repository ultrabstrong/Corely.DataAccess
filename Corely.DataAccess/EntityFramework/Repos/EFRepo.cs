using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Entities;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.EntityFramework.Repos;

public class EFRepo<TEntity>
    : EFReadonlyRepo<TEntity>, IRepo<TEntity>
    where TEntity : class
{
    protected readonly DbContext DbContext;

    public EFRepo(
        ILogger<EFRepo<TEntity>> logger,
        DbContext dbContext)
        : base(logger, dbContext)
    {
        DbContext = dbContext.ThrowIfNull(nameof(dbContext));
        Logger.LogDebug("{RepoType} created for {EntityType}", GetType().Name.Split('`')[0], typeof(TEntity).Name);
    }

    private bool ShouldSaveChanges() => !EFUoWScope.IsActive && DbContext.ChangeTracker.HasChanges();

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var newEntity = await DbSet.AddAsync(entity, cancellationToken);
        if (ShouldSaveChanges())
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        return newEntity.Entity;
    }

    public virtual async Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        if (ShouldSaveChanges())
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (typeof(IHasModifiedUtc).IsAssignableFrom(typeof(TEntity)))
        {
            ((IHasModifiedUtc)entity).ModifiedUtc = DateTime.UtcNow;
        }

        // Determine primary key properties for entity type
        var entityType = DbContext.Model.FindEntityType(typeof(TEntity));
        var key = entityType?.FindPrimaryKey();

        if (key == null || key.Properties.Count == 0)
        {
            // Fallback: attach and mark modified (no key metadata)
            DbSet.Attach(entity);
            DbContext.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            // Try to find an already tracked instance with the same key values
            var tracked = DbSet.Local.FirstOrDefault(local =>
                // Support composite keys
                key.Properties.All(pkProp =>
                {
                    var clrProp = typeof(TEntity).GetProperty(pkProp.Name);
                    if (clrProp == null)
                        return false;
                    var localVal = clrProp.GetValue(local);
                    var newVal = clrProp.GetValue(entity);
                    return Equals(localVal, newVal);
                }));

            if (tracked != null)
            {
                // Update tracked instance values
                DbContext.Entry(tracked).CurrentValues.SetValues(entity);
            }
            else
            {
                // Attach incoming entity and mark as modified to update all scalar properties
                DbSet.Attach(entity);
                DbContext.Entry(entity).State = EntityState.Modified;
            }
        }

        if (ShouldSaveChanges())
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        if (ShouldSaveChanges())
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
