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

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var newEntity = await DbSet.AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        return newEntity.Entity;
    }

    public virtual async Task CreateAsync(params TEntity[] entities)
    {
        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
    }

    public virtual async Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, Func<TEntity, bool> query, CancellationToken cancellationToken = default)
    {
        if (typeof(IHasModifiedUtc).IsAssignableFrom(typeof(TEntity)))
        {
            ((IHasModifiedUtc)entity).ModifiedUtc = DateTime.UtcNow;
        }

        var existingEntity = DbSet.Local.FirstOrDefault(query);
        if (existingEntity == null)
        {
            // attach new entity instance to local context for tracking
            DbSet.Attach(entity).State = EntityState.Modified;
        }
        else
        {
            // update existing tracked entity instance with new entity values
            DbSet.Entry(existingEntity).CurrentValues.SetValues(entity);
        }
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
