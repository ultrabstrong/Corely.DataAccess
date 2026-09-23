using System.Linq.Expressions;
using System.Reflection;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Mock.Repos;

internal sealed class MockUpdateSetters<TEntity>(TEntity entity) : IUpdateSetters<TEntity>
{
    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value
    )
    {
        ResolveProperty(property).SetValue(entity, value);
        return this;
    }

    public IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> valueExpression
    )
    {
        ArgumentNullException.ThrowIfNull(valueExpression);
        ResolveProperty(property).SetValue(entity, valueExpression.Compile()(entity));
        return this;
    }

    private static PropertyInfo ResolveProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property
    )
    {
        ArgumentNullException.ThrowIfNull(property);

        var body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : property.Body;

        if (body is not MemberExpression { Member: PropertyInfo info })
        {
            throw new ArgumentException(
                $"SetProperty expects a property access expression, but received '{property.Body}'.",
                nameof(property)
            );
        }

        return info;
    }
}
