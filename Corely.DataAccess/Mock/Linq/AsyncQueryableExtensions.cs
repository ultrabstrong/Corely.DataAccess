namespace Corely.DataAccess.Mock.Linq;

internal static class AsyncQueryableExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public IQueryable<T> AsAsyncQueryable() => new AsyncEnumerable<T>(source);
    }
}
