namespace Corely.DataAccess.Mock.Linq;

// Adapts in-memory IEnumerable<T> to support EF-style async operators (e.g., SumAsync/CountAsync)
// by providing an IAsyncQueryProvider implementation that evaluates synchronously
// and wraps the result in a completed Task.
internal static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
        new AsyncEnumerable<T>(source);
}
