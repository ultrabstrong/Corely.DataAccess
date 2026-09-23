using Corely.DataAccess.Mock.Linq;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.Mock.Linq;

public class AsyncQueryProviderTests
{
    private static IQueryable<int> GetBaseQueryable(params int[] data) => data.AsQueryable();

    [Fact]
    public async Task ExecuteAsync_Wraps_Sync_Result_In_Task_For_ToListAsync()
    {
        var source = GetBaseQueryable(1, 2, 3);
        var asyncQueryable = new AsyncEnumerable<int>(source.Expression);

        var list = await EntityFrameworkQueryableExtensions.ToListAsync(asyncQueryable);

        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public async Task SumAsync_Uses_Async_Provider_Successfully()
    {
        var data = new[] { 1, 2, 3, 4 };
        var query = data.AsAsyncQueryable();

        var sum = await query.SumAsync(x => x);

        Assert.Equal(10, sum);
    }

    [Fact]
    public void CreateQuery_NonGeneric_Returns_AsyncQueryable()
    {
        var baseQuery = GetBaseQueryable(1, 2);
        var provider = new AsyncQueryProvider(baseQuery.Provider);

        var nonGenericQuery = provider.CreateQuery(baseQuery.Expression);

        Assert.IsType<IAsyncEnumerable<int>>(nonGenericQuery, exactMatch: false);
        Assert.IsType<AsyncEnumerable<int>>(nonGenericQuery);
    }

    [Fact]
    public void Execute_Synchronous_Works()
    {
        var baseQuery = GetBaseQueryable(1, 2, 3);
        var provider = new AsyncQueryProvider(baseQuery.Provider);
        var whereExpr = baseQuery.Where(x => x > 1).Expression;

        var result = provider.Execute<IEnumerable<int>>(whereExpr);

        Assert.NotNull(result);
        Assert.Equal([2, 3], result);
    }
}
