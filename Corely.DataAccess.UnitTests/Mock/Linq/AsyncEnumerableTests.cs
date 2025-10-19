using Corely.DataAccess.Mock.Linq;
using Microsoft.EntityFrameworkCore;

namespace Corely.DataAccess.UnitTests.Mock.Linq;

public class AsyncEnumerableTests
{
    [Fact]
    public async Task GetAsyncEnumerator_Iterates_All_Items()
    {
        // Arrange
        var data = new[] { 1, 2, 3 };
        var asyncEnumerable = new AsyncEnumerable<int>(data);

        // Act
        var results = new List<int>();
        await foreach (var i in asyncEnumerable)
        {
            results.Add(i);
        }

        // Assert
        Assert.Equal(data, results);
    }

    [Fact]
    public void Provider_Is_AsyncQueryProvider()
    {
        // Arrange
        var data = new[] { 1, 2, 3 };
        var asyncQueryable = new AsyncEnumerable<int>(data);

        // Act
        var provider = ((IQueryable<int>)asyncQueryable).Provider;

        // Assert
        Assert.IsType<AsyncQueryProvider>(provider);
    }

    [Fact]
    public async Task Linq_Composition_And_ToListAsync_Work()
    {
        // Arrange
        var data = new[] { 1, 2, 3, 4 };
        var asyncQueryable = new AsyncEnumerable<int>(data);

        // Act
        var query = ((IQueryable<int>)asyncQueryable).Where(x => x > 2).Select(x => x * 2);
        var result = await query.ToListAsync();

        // Assert
        Assert.Equal([6, 8], result);
    }
}
