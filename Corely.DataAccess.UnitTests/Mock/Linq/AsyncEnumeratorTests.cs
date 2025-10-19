using System.Collections;
using Corely.DataAccess.Mock.Linq;

namespace Corely.DataAccess.UnitTests.Mock.Linq;

public class AsyncEnumeratorTests
{
    [Fact]
    public async Task MoveNextAsync_And_Current_Work()
    {
        // Arrange
        var source = new List<string> { "a", "b" };
        await using var enumerator = new AsyncEnumerator<string>(source.GetEnumerator());

        // Act & Assert
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("a", enumerator.Current);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("b", enumerator.Current);

        Assert.False(await enumerator.MoveNextAsync());
    }

    private sealed class TestEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public bool Disposed { get; private set; }

        public TestEnumerator(IEnumerable<T> source)
        {
            _inner = source.GetEnumerator();
        }

        public T Current => _inner.Current;
        object IEnumerator.Current => Current!;

        public bool MoveNext() => _inner.MoveNext();

        public void Reset() => (_inner as IEnumerator)?.Reset();

        public void Dispose()
        {
            Disposed = true;
            _inner.Dispose();
        }
    }

    [Fact]
    public async Task DisposeAsync_Disposes_Inner_Enumerator()
    {
        // Arrange
        var inner = new TestEnumerator<int>([1]);
        var asyncEnumerator = new AsyncEnumerator<int>(inner);

        // Act
        await asyncEnumerator.DisposeAsync();

        // Assert
        Assert.True(inner.Disposed);
    }
}
