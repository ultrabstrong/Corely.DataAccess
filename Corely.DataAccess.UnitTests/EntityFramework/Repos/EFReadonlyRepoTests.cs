using AutoFixture;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.EntityFramework.Repos;

public class EFReadonlyRepoTests : ReadonlyRepoTestsBase
{
    private readonly DbContextFixture _dbContext;
    private readonly EFReadonlyRepo<DbContextFixture, EntityFixture> _efReadonlyRepo;

    public EFReadonlyRepoTests()
    {
        _dbContext = GetDbContext();

        _efReadonlyRepo = new(
            Moq.Mock.Of<ILogger<EFReadonlyRepo<DbContextFixture, EntityFixture>>>(),
            _dbContext
        );
    }

    private static DbContextFixture GetDbContext()
    {
        var fixture = new Fixture();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(databaseName: fixture.Create<string>())
            .Options;

        var dbContext = new DbContextFixture(options);

        var entityList = fixture.CreateMany<EntityFixture>(5).ToList();
        foreach (var entity in entityList)
        {
            dbContext.Entities.Add(entity);
        }
        dbContext.SaveChanges();

        return dbContext;
    }

    protected override IReadonlyRepo<EntityFixture> ReadonlyRepo => _efReadonlyRepo;

    protected override int FillRepoAndReturnId() =>
        _dbContext.Set<EntityFixture>().Skip(1).First().Id;

    [Fact]
    public void EFReadonlyRepo_Implements_Public_Readonly_Interface()
    {
        Assert.IsAssignableFrom<IReadonlyRepo<EntityFixture>>(_efReadonlyRepo);
    }

    [Fact]
    public async Task EvaluateAsync_Allows_Aggregates()
    {
        // Arrange
        var expected = await _dbContext.Set<EntityFixture>().SumAsync(e => e.Id);

        // Act
        var sum = await _efReadonlyRepo.EvaluateAsync((q, ct) => q.SumAsync(e => e.Id, ct));

        // Assert
        Assert.Equal(expected, sum);
    }

    [Fact]
    public async Task QueryAsync_Allows_Projections()
    {
        // Act
        var ids = await _efReadonlyRepo.QueryAsync(q => q.OrderBy(e => e.Id).Select(e => e.Id));

        // Assert
        var expected = await _dbContext
            .Set<EntityFixture>()
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToListAsync();
        Assert.Equal(expected, ids);
    }
}
