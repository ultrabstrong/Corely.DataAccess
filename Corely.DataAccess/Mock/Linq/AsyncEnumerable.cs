using System.Linq.Expressions;

namespace Corely.DataAccess.Mock.Linq;

internal sealed class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable) { }

    public AsyncEnumerable(Expression expression)
        : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new AsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

    IQueryProvider IQueryable.Provider => new AsyncQueryProvider(this);
}
