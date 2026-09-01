using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore.Query;

namespace Corely.DataAccess.EntityFramework.Repos;

/// <summary>
/// Relays <see cref="IUpdateSetters{TEntity}"/> calls to EF Core's own setter builder.
/// </summary>
/// <remarks>
/// This adapter is the only place in the library that names EF's setter type, so an EF revision of
/// it is contained here rather than reaching <see cref="IRepo{TEntity}"/> and its consumers.
/// </remarks>
internal sealed class EFUpdateSetters<TEntity>(UpdateSettersBuilder<TEntity> builder)
    : IUpdateSetters<TEntity>
{
    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value
    )
    {
        builder.SetProperty(property, value);
        return this;
    }

    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> valueExpression
    )
    {
        builder.SetProperty(property, valueExpression);
        return this;
    }
}
