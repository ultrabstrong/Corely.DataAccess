using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Mock.Repos;

internal sealed class MockUpdateSetters<TEntity>(TEntity entity) : IUpdateSetters<TEntity>
{
    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value
    )
    {
        property.SelectedProperty().SetValue(entity, value);
        return this;
    }

    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> valueExpression
    )
    {
        ArgumentNullException.ThrowIfNull(valueExpression);
        property.SelectedProperty().SetValue(entity, valueExpression.Compile()(entity));
        return this;
    }
}
