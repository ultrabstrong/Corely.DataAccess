using System.Linq.Expressions;

namespace Corely.DataAccess.Interfaces.Repos;

public interface IUpdateSetters<TEntity>
{
    IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value
    );

    IUpdateSetters<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> valueExpression
    );
}
