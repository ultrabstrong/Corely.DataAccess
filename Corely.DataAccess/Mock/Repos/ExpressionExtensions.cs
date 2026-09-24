using System.Linq.Expressions;
using System.Reflection;

namespace Corely.DataAccess.Mock.Repos;

internal static class ExpressionExtensions
{
    extension<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        public PropertyInfo SelectedProperty()
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
}
