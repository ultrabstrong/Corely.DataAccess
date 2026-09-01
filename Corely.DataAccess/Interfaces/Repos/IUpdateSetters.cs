using System.Linq.Expressions;

namespace Corely.DataAccess.Interfaces.Repos;

/// <summary>
/// Collects the property assignments applied by
/// <see cref="IRepo{TEntity}.ExecuteUpdateAsync"/>.
/// </summary>
/// <remarks>
/// This exists so <see cref="IRepo{TEntity}"/> does not expose an EF Core type. EF has revised the
/// setter type once already (<c>SetPropertyCalls</c> in EF 9 became <c>UpdateSettersBuilder</c> in
/// EF 10), and every such revision would otherwise be a breaking change to this interface and to
/// every consumer of it.
/// </remarks>
public interface IUpdateSetters<TEntity>
{
    /// <summary>Assigns <paramref name="value"/> to the property named by
    /// <paramref name="property"/>.</summary>
    IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value
    );

    /// <summary>Assigns the result of <paramref name="valueExpression"/>, evaluated in the
    /// database against each matched row, to the property named by
    /// <paramref name="property"/>.</summary>
    IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> valueExpression
    );
}
