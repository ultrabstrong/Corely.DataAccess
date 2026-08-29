using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Corely.DataAccess.Mock.Linq;

/// <summary>
/// Interprets the <see cref="SetPropertyCalls{TSource}"/> chain that EF Core would normally
/// translate to SQL, so in-memory repos can apply the same updates.
/// </summary>
/// <remarks>
/// EF never executes <see cref="SetPropertyCalls{TSource}"/> - it only reads the expression tree.
/// This walks that tree and applies each assignment by reflection instead. Only the subset EF
/// itself supports is handled; anything else throws rather than silently diverging from EF.
/// <para>
/// Assigning the same property twice in one chain is provider-dependent (EF emits both
/// assignments and each database picks a winner), so callers must not rely on it.
/// </para>
/// </remarks>
internal static class SetPropertyInterpreter<TEntity>
{
    internal sealed record PropertySetter(PropertyInfo Property, Func<TEntity, object?> GetValue);

    public static List<PropertySetter> Parse(
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setProperties
    )
    {
        ArgumentNullException.ThrowIfNull(setProperties);

        var setters = new List<PropertySetter>();
        var current = setProperties.Body;

        while (current is MethodCallExpression call)
        {
            if (call.Method.Name != "SetProperty" || call.Arguments.Count != 2)
            {
                throw new NotSupportedException(
                    $"Only SetProperty(property, value) calls are supported; found '{call.Method.Name}'."
                );
            }

            setters.Add(
                new PropertySetter(
                    ResolveProperty(Unquote(call.Arguments[0])),
                    BuildValueAccessor(Unquote(call.Arguments[1]))
                )
            );

            // SetProperty is an instance method, so the receiver is the previous link in the chain.
            current =
                call.Object ?? throw new NotSupportedException("Malformed SetProperty chain.");
        }

        if (current is not ParameterExpression)
        {
            throw new NotSupportedException(
                "SetProperty chain must start from the lambda parameter."
            );
        }

        // Chain was walked outermost-first; restore author order so later writes win.
        setters.Reverse();
        return setters;
    }

    public static void Apply(TEntity entity, IReadOnlyList<PropertySetter> setters)
    {
        foreach (var setter in setters)
        {
            setter.Property.SetValue(entity, setter.GetValue(entity));
        }
    }

    private static Expression Unquote(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Quote } quote
            ? quote.Operand
            : expression;

    private static Expression StripConvert(Expression expression) =>
        expression
            is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } convert
            ? convert.Operand
            : expression;

    private static PropertyInfo ResolveProperty(Expression expression)
    {
        if (
            expression is LambdaExpression lambda
            && StripConvert(lambda.Body) is MemberExpression member
            && member.Member is PropertyInfo property
        )
        {
            return property;
        }

        throw new NotSupportedException(
            $"Only direct property selectors are supported; found '{expression}'."
        );
    }

    private static Func<TEntity, object?> BuildValueAccessor(Expression expression)
    {
        // SetProperty has two overloads: a constant value, or a value computed from the entity.
        if (
            expression is LambdaExpression lambda
            && lambda.Parameters.Count == 1
            && lambda.Parameters[0].Type == typeof(TEntity)
        )
        {
            var compiled = lambda.Compile();
            return entity => compiled.DynamicInvoke(entity);
        }

        var value = Expression
            .Lambda(Expression.Convert(expression, typeof(object)))
            .Compile()
            .DynamicInvoke();

        return _ => value;
    }
}
