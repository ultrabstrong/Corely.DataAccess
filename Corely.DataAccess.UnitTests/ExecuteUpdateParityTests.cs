using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests;

/// <summary>
/// MockRepo re-implements EF's SetProperty semantics by hand, so every behaviour is asserted
/// against both implementations. If the mock drifts from EF, these fail.
/// </summary>
public sealed class ExecuteUpdateParityTests : IDisposable
{
    private static readonly DateTime _seedCreated = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _stamp = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextFixture _dbContext;
    private readonly EFRepo<DbContextFixture, EntityFixture> _efRepo;
    private readonly MockRepo<EntityFixture> _mockRepo = new();

    public ExecuteUpdateParityTests()
    {
        // Sqlite (not InMemory) - the InMemory provider cannot execute set-based updates.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>().UseSqlite(_connection).Options
        );
        _dbContext.Database.EnsureCreated();

        _efRepo = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            _dbContext,
            new EFUoWProvider()
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
    public async Task ExecuteUpdate_SetsConstantValue_OnMatchingRowsOnly()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteUpdateAsync(
                e => e.Id > 2,
                s => s.SetProperty(e => e.ModifiedUtc, _stamp)
            );

            Assert.Equal(2, affected);

            var rows = Read(repo);
            Assert.All(rows.Where(e => e.Id > 2), e => Assert.Equal(_stamp, e.ModifiedUtc));
            Assert.All(rows.Where(e => e.Id <= 2), e => Assert.Null(e.ModifiedUtc));
        }
    }

    [Fact]
    public async Task ExecuteUpdate_ComputesValueFromEntity()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteUpdateAsync(
                e => e.Id <= 2,
                s => s.SetProperty(e => e.CreatedUtc, e => e.CreatedUtc.AddDays(1))
            );

            Assert.Equal(2, affected);

            var rows = Read(repo);
            Assert.All(
                rows.Where(e => e.Id <= 2),
                e => Assert.Equal(_seedCreated.AddDays(1), e.CreatedUtc)
            );
            Assert.All(rows.Where(e => e.Id > 2), e => Assert.Equal(_seedCreated, e.CreatedUtc));
        }
    }

    [Fact]
    public async Task ExecuteUpdate_AppliesEveryPropertyInChain()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteUpdateAsync(
                e => e.Id == 1,
                s =>
                    s.SetProperty(e => e.ModifiedUtc, _stamp).SetProperty(e => e.CreatedUtc, _stamp)
            );

            Assert.Equal(1, affected);

            var row = Read(repo).Single(e => e.Id == 1);
            Assert.Equal(_stamp, row.ModifiedUtc);
            Assert.Equal(_stamp, row.CreatedUtc);
        }
    }

    [Fact]
    public async Task ExecuteUpdate_ReturnsZero_AndChangesNothing_WhenNoRowsMatch()
    {
        foreach (var repo in BothRepos)
        {
            var affected = await repo.ExecuteUpdateAsync(
                e => e.Id == 999,
                s => s.SetProperty(e => e.ModifiedUtc, _stamp)
            );

            Assert.Equal(0, affected);
            Assert.All(Read(repo), e => Assert.Null(e.ModifiedUtc));
        }
    }

    [Fact]
    public async Task ExecuteUpdate_SetsNull()
    {
        foreach (var repo in BothRepos)
        {
            await repo.ExecuteUpdateAsync(
                e => e.Id == 1,
                s => s.SetProperty(e => e.ModifiedUtc, _stamp)
            );
            await repo.ExecuteUpdateAsync(
                e => e.Id == 1,
                s => s.SetProperty(e => e.ModifiedUtc, (DateTime?)null)
            );

            Assert.Null(Read(repo).Single(e => e.Id == 1).ModifiedUtc);
        }
    }

    [Fact]
    public async Task ExecuteUpdate_DoesNotApplyModifiedUtcConvention()
    {
        // Set-based updates bypass change tracking in EF; the mock must not "helpfully" differ.
        foreach (var repo in BothRepos)
        {
            await repo.ExecuteUpdateAsync(
                e => e.Id == 1,
                s => s.SetProperty(e => e.CreatedUtc, _stamp)
            );

            Assert.Null(Read(repo).Single(e => e.Id == 1).ModifiedUtc);
        }
    }

    [Fact]
    public async Task ExecuteUpdate_Throws_WhenSelectorIsNotADirectProperty()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _mockRepo.ExecuteUpdateAsync(
                e => e.Id == 1,
                s => s.SetProperty(e => e.NavigationProperty!.Id, 7)
            )
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
