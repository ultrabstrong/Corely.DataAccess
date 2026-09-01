using System.Linq.Expressions;
using System.Reflection;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.DataAccess.Mock.Repos;

/// <summary>
/// Applies <see cref="IUpdateSetters{TEntity}"/> calls directly to an entity instance.
/// </summary>
/// <remarks>
/// EF never runs the setters - it reads them to build an UPDATE statement - so the mock has to
/// perform the assignment itself. Value expressions are compiled and evaluated against the entity,
/// which is the in-memory equivalent of evaluating them in the database per row.
/// </remarks>
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

        // Unwrap the conversion the compiler inserts when the property type differs from TProperty.
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
