using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests;

public sealed class ExecuteDeleteParityTests : IDisposable
{
    private static readonly DateTime _seedCreated = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextFixture _dbContext;
    private readonly EFUoWProvider _uow = new();
    private readonly EFRepo<DbContextFixture, EntityFixture> _efRepo;
    private readonly MockRepo<EntityFixture> _mockRepo = new();

    public ExecuteDeleteParityTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>().UseSqlite(_connection).Options
        );
        _dbContext.Database.EnsureCreated();

        _efRepo = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            _dbContext,
            _uow,
            TimeProvider.System
        );

        Seed();
    }

    private void Seed()
    {
        for (var id = 1; id <= 4; id++)
        {
            _dbContext.Add(new EntityFixture { Id = id, CreatedUtc = _seedCreated });
            _mockRepo.Entities.Add(new EntityFixture { Id = id, CreatedUtc = _seedCreated });
        }
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();
    }

    private IEnumerable<IRepo<EntityFixture>> BothRepos => [_efRepo, _mockRepo];

    private List<EntityFixture> Read(IRepo<EntityFixture> repo) =>
        repo == _mockRepo
            ? [.. _mockRepo.Entities.OrderBy(e => e.Id)]
            : [.. _dbContext.Set<EntityFixture>().AsNoTracking().OrderBy(e => e.Id)];

    [Fact]
    public async Task ExecuteDelete_DeletesMatchingRowsOnly_AndReturnsCount()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteDeleteAsync(e => e.Id > 2);

            Assert.Equal(2, affected);
            Assert.Equal([1, 2], Read(repo).Select(e => e.Id));
        }
    }

    [Fact]
    public async Task ExecuteDelete_ReturnsZero_AndDeletesNothing_WhenNoRowsMatch()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteDeleteAsync(e => e.Id == 999);

            Assert.Equal(0, affected);
            Assert.Equal([1, 2, 3, 4], Read(repo).Select(e => e.Id));
        }
    }

    [Fact]
    public async Task ExecuteDelete_EmptiesSet_WhenEveryRowMatches()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteDeleteAsync(e => e.Id > 0);

            Assert.Equal(4, affected);
            Assert.Empty(Read(repo));
        }
    }

    [Fact]
    public async Task ExecuteDelete_Throws_WhenQueryIsNull()
    {
        foreach (var repo in BothRepos)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.ExecuteDeleteAsync(null!));
        }
    }

    [Fact]
    public async Task ExecuteDelete_IsUndone_WhenUnitOfWorkRollsBack()
    {
        await _uow.BeginAsync();

        var affected = await _efRepo.ExecuteDeleteAsync(e => e.Id > 2);
        await _uow.RollbackAsync();

        Assert.Equal(2, affected);
        Assert.Equal([1, 2, 3, 4], Read(_efRepo).Select(e => e.Id));
    }

    [Fact]
    public async Task ExecuteDelete_IsKept_WhenUnitOfWorkCommits()
    {
        await _uow.BeginAsync();

        await _efRepo.ExecuteDeleteAsync(e => e.Id > 2);
        await _uow.CommitAsync();

        Assert.Equal([1, 2], Read(_efRepo).Select(e => e.Id));
    }

    [Fact]
    public async Task ExecuteDelete_LeavesLoadedEntitiesTracked()
    {
        var loaded = _dbContext.Set<EntityFixture>().Single(e => e.Id == 1);

        await _efRepo.ExecuteDeleteAsync(e => e.Id == 1);

        Assert.Equal(EntityState.Unchanged, _dbContext.Entry(loaded).State);
        Assert.DoesNotContain(1, Read(_efRepo).Select(e => e.Id));
    }

    [Fact]
    public async Task ExecuteDelete_FollowsDatabaseForeignKeyRules_NotClientSideCascade()
    {
        var principal = new NavigationPropertyFixture { Id = 10, CreatedUtc = _seedCreated };
        _dbContext.Add(principal);
        _dbContext.Set<EntityFixture>().Single(e => e.Id == 1).NavigationProperty = principal;
        _dbContext.SaveChanges();

        var principalRepo = new EFRepo<DbContextFixture, NavigationPropertyFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, NavigationPropertyFixture>>>(),
            _dbContext,
            _uow,
            TimeProvider.System
        );

        await Assert.ThrowsAsync<SqliteException>(() =>
            principalRepo.ExecuteDeleteAsync(n => n.Id == 10)
        );
    }

    public void Dispose()
    {
        _uow.Dispose();
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
