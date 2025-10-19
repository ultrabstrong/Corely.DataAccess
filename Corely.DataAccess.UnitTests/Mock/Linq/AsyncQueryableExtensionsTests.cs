using Corely.DataAccess.Mock.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Corely.DataAccess.UnitTests.Mock.Linq;

public class AsyncQueryableExtensionsTests
{
    [Fact]
    public async Task AsAsyncQueryable_ProvidesAsyncProvider_And_ToListAsync_Works()
    {
        // Arrange
        var data = new[] { 1, 2, 3 };

        // Act
        var query = data.AsAsyncQueryable();
        var list = await query.ToListAsync();

        // Assert
        Assert.IsType<IAsyncEnumerable<int>>(query, exactMatch: false);
        Assert.IsType<IAsyncQueryProvider>(query.Provider, exactMatch: false);
        Assert.IsType<AsyncQueryProvider>(query.Provider);
        Assert.Equal(data, list);
    }
}
