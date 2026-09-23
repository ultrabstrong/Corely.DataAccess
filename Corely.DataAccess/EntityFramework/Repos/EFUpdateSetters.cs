using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;
using Microsoft.EntityFrameworkCore.Query;

namespace Corely.DataAccess.EntityFramework.Repos;

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
