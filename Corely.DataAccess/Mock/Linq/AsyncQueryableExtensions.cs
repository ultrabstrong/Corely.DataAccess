namespace Corely.DataAccess.Mock.Linq;

internal static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
        new AsyncEnumerable<T>(source);
}
