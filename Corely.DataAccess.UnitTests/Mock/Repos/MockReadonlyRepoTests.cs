using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;

namespace Corely.DataAccess.UnitTests.Mock.Repos;

public class MockReadonlyRepoTests : ReadonlyRepoTestsBase
{
    private readonly MockRepo<EntityFixture> _mockRepo = new();
    private readonly MockReadonlyRepo<EntityFixture> _mockReadonlyRepo;

    public MockReadonlyRepoTests()
    {
        _mockReadonlyRepo = new MockReadonlyRepo<EntityFixture>(_mockRepo);
    }

    protected override IReadonlyRepo<EntityFixture> ReadonlyRepo => _mockReadonlyRepo;

    protected override int FillRepoAndReturnId()
    {
        var entityList = Fixture.CreateMany<EntityFixture>(5).ToList();
        foreach (var entity in entityList)
        {
            _mockRepo.CreateAsync(entity);
        }

        return entityList[2].Id;
    }

    [Fact]
    public async Task EvaluateAsync_Allows_Aggregates()
    {
        // Arrange
        var expected = _mockRepo.Entities.Sum(e => e.Id);

        // Act
        var sum = await _mockReadonlyRepo.EvaluateAsync(
            (q, ct) => Task.FromResult(q.Sum(e => e.Id))
        );

        // Assert
        Assert.Equal(expected, sum);
    }

    [Fact]
    public async Task QueryAsync_Allows_Projections()
    {
        // Act
        var ids = await _mockReadonlyRepo.QueryAsync(q => q.OrderBy(e => e.Id).Select(e => e.Id));

        // Assert
        var expected = _mockRepo.Entities.OrderBy(e => e.Id).Select(e => e.Id).ToList();
        Assert.Equal(expected, ids);
    }
}
