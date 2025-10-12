using Corely.Common.Extensions;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Entities;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.EntityFramework.Repos;

internal sealed class EFRepo<TContext, TEntity> : EFReadonlyRepo<TContext, TEntity>, IRepo<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    private readonly EFUoWProvider _uow;

    public EFRepo(ILogger<EFRepo<TContext, TEntity>> logger, TContext dbContext, EFUoWProvider uow)
        : base(logger, dbContext)
    {
        _uow = uow.ThrowIfNull(nameof(uow));
        Logger.LogTrace(
            "{RepoType} created for {EntityType} on {ContextType}",
            GetType().Name.Split('`')[0],
            typeof(TEntity).Name,
            typeof(TContext).Name
        );
    }

    private bool ShouldSaveChanges() => (!_uow.IsActive) && DbContext.ChangeTracker.HasChanges();

    public async Task<TEntity> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        if (_uow.IsActive)
            _uow.Register(DbContext);
        var newEntity = await DbSet.AddAsync(entity, cancellationToken);
        if (ShouldSaveChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
        return newEntity.Entity;
    }

    public async Task CreateAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        if (_uow.IsActive)
            _uow.Register(DbContext);
        await DbSet.AddRangeAsync(entities, cancellationToken);
        if (ShouldSaveChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (_uow.IsActive)
            _uow.Register(DbContext);

        if (typeof(IHasModifiedUtc).IsAssignableFrom(typeof(TEntity)))
            ((IHasModifiedUtc)entity).ModifiedUtc = DateTime.UtcNow;

        var entityType = DbContext.Model.FindEntityType(typeof(TEntity));
        var key = entityType?.FindPrimaryKey();

        if (key == null || key.Properties.Count == 0)
        {
            DbSet.Attach(entity);
            DbContext.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            var tracked = DbSet.Local.FirstOrDefault(local =>
                key.Properties.All(pkProp =>
                {
                    var clrProp = typeof(TEntity).GetProperty(pkProp.Name);
                    if (clrProp == null)
                        return false;
                    var localVal = clrProp.GetValue(local);
                    var newVal = clrProp.GetValue(entity);
                    return Equals(localVal, newVal);
                })
            );

            if (tracked != null)
                DbContext.Entry(tracked).CurrentValues.SetValues(entity);
            else
            {
                DbSet.Attach(entity);
                DbContext.Entry(entity).State = EntityState.Modified;
            }
        }

        if (ShouldSaveChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (_uow.IsActive)
            _uow.Register(DbContext);
        DbSet.Remove(entity);
        if (ShouldSaveChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }
}
