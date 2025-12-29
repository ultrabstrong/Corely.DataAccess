using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Corely.DataAccess.Mock.Linq;

internal sealed class AsyncQueryProvider : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public AsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IQueryable CreateQuery(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var elementType =
            expression.Type.GetGenericArguments().FirstOrDefault()
            ?? expression
                .Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType)
                ?.GetGenericArguments()
                .FirstOrDefault()
            ?? typeof(object);
        var asyncEnumerableType = typeof(AsyncEnumerable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(asyncEnumerableType, expression)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return new AsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerResultType = resultType.GetGenericArguments()[0];

            // Execute synchronously using the inner provider
            var syncResult = _inner.Execute(expression);

            // Wrap in Task.FromResult(innerResultType)
            var taskFromResult = typeof(Task)
                .GetMethods()
                .First(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod)
                .MakeGenericMethod(innerResultType);

            return (TResult)taskFromResult.Invoke(null, [syncResult])!;
        }

        var nonTaskResult = _inner.Execute(expression);
        return (TResult)nonTaskResult!;
    }
}
