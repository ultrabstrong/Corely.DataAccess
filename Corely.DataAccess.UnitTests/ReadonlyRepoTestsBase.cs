using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace Corely.DataAccess.UnitTests;

public abstract class ReadonlyRepoTestsBase
{
    protected readonly Fixture Fixture = new();

    protected abstract IReadonlyRepo<EntityFixture> ReadonlyRepo { get; }

    protected abstract IEnumerable<EntityFixture> Entities { get; }

    protected abstract int FillRepoAndReturnId();

    [Fact]
    public async Task GetAsync_ReturnsEntity_WIthIdLookup()
    {
        var id = FillRepoAndReturnId();
        var result = await ReadonlyRepo.GetAsync(e => e.Id == id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsEntity_WithVerboseIdLookup()
    {
        var id = FillRepoAndReturnId();
        var result = await ReadonlyRepo.GetAsync(u => u.Id == id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetAsync_Uses_OrderBy()
    {
        var orderByMock =
            new Mock<Func<IQueryable<EntityFixture>, IOrderedQueryable<EntityFixture>>>();
        orderByMock
            .Setup(m => m(It.IsAny<IQueryable<EntityFixture>>()))
            .Returns((IQueryable<EntityFixture> q) => q.OrderBy(u => u.Id));

        await ReadonlyRepo.GetAsync(u => u.Id == 1, orderBy: orderByMock.Object);

        orderByMock.Verify(m => m(It.IsAny<IQueryable<EntityFixture>>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Uses_Include()
    {
        var includeMock = new Mock<Func<IQueryable<EntityFixture>, IQueryable<EntityFixture>>>();
        includeMock
            .Setup(m => m(It.IsAny<IQueryable<EntityFixture>>()))
            .Returns((IQueryable<EntityFixture> q) => q.Include(u => u.NavigationProperty));

        await ReadonlyRepo.GetAsync(u => u.Id == 1, include: includeMock.Object);

        includeMock.Verify(m => m(It.IsAny<IQueryable<EntityFixture>>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Throws_WithNullQuery()
    {
        var ex = await Record.ExceptionAsync(() => ReadonlyRepo.GetAsync(null!));
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task AnyAsync_ReturnsTrue_WhenEntityExists()
    {
        var id = FillRepoAndReturnId();
        var result = await ReadonlyRepo.AnyAsync(u => u.Id == id);
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_Throws_WithNullQuery()
    {
        var ex = await Record.ExceptionAsync(() => ReadonlyRepo.AnyAsync(null!));
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task CountAsync_ReturnsTotal_WhenQueryIsNull()
    {
        FillRepoAndReturnId();
        var total = await ReadonlyRepo.CountAsync();
        Assert.True(total > 0);
    }

    [Fact]
    public async Task CountAsync_ReturnsFilteredCount_WhenQueryProvided()
    {
        var id = FillRepoAndReturnId();
        var count = await ReadonlyRepo.CountAsync(e => e.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllEntities_WhenQueryIsNull()
    {
        FillRepoAndReturnId();
        var result = await ReadonlyRepo.ListAsync();
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsMatchingEntities_WhenQueryIsNotNull()
    {
        var id = FillRepoAndReturnId();
        var result = await ReadonlyRepo.ListAsync(u => u.Id == id);
        Assert.NotEmpty(result);
        Assert.All(result, u => Assert.Equal(id, u.Id));
    }

    [Fact]
    public async Task ListAsync_Uses_OrderBy()
    {
        var orderByMock =
            new Mock<Func<IQueryable<EntityFixture>, IOrderedQueryable<EntityFixture>>>();
        orderByMock
            .Setup(m => m(It.IsAny<IQueryable<EntityFixture>>()))
            .Returns((IQueryable<EntityFixture> q) => q.OrderBy(u => u.Id));

        await ReadonlyRepo.ListAsync(orderBy: orderByMock.Object);

        orderByMock.Verify(m => m(It.IsAny<IQueryable<EntityFixture>>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Uses_Include()
    {
        var includeMock = new Mock<Func<IQueryable<EntityFixture>, IQueryable<EntityFixture>>>();
        includeMock
            .Setup(m => m(It.IsAny<IQueryable<EntityFixture>>()))
            .Returns((IQueryable<EntityFixture> q) => q.Include(u => u.NavigationProperty));

        await ReadonlyRepo.ListAsync(include: includeMock.Object);

        includeMock.Verify(m => m(It.IsAny<IQueryable<EntityFixture>>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_Allows_Aggregates()
    {
        // Arrange
        var expected = Entities.Sum(e => e.Id);

        // Act
        var sum = await ReadonlyRepo.EvaluateAsync((q, ct) => Task.FromResult(q.Sum(e => e.Id)));

        // Assert
        Assert.Equal(expected, sum);
    }

    [Fact]
    public async Task EvaluateAsync_Allows_AsyncAggregates()
    {
        // Arrange
        var expected = Entities.Sum(e => e.Id);

        // Act
        var sum = await ReadonlyRepo.EvaluateAsync(
            (q, ct) => q.SumAsync(e => e.Id, cancellationToken: ct)
        );

        // Assert
        Assert.Equal(expected, sum);
    }

    [Fact]
    public async Task QueryAsync_Allows_Projections()
    {
        // Act
        var ids = await ReadonlyRepo.QueryAsync(q => q.OrderBy(e => e.Id).Select(e => e.Id));

        // Assert
        var expected = Entities.OrderBy(e => e.Id).Select(e => e.Id).ToList();
        Assert.Equal(expected, ids);
    }

    [Fact]
    public async Task QueryAsync_Allows_AsyncProjections()
    {
        // Act: verify the queryable supports async provider inside the projection builder
        var ids = await ReadonlyRepo.QueryAsync(q =>
        {
            Assert.IsType<IAsyncQueryProvider>(q.Provider, exactMatch: false);
            return q.OrderBy(e => e.Id).Select(e => e.Id);
        });

        // Assert
        var expected = Entities.OrderBy(e => e.Id).Select(e => e.Id).ToList();
        Assert.Equal(expected, ids);
    }
}
