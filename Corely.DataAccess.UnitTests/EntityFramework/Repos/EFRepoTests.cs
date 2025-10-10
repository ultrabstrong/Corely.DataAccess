using AutoFixture;
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.DataAccess.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.EntityFramework.Repos;

public class EFRepoTests : RepoTestsBase
{
    private readonly EFRepo<DbContextFixture, EntityFixture> _efRepo;
    private readonly DbContextFixture _dbContext;
    private readonly string _dbName;
    private readonly IUnitOfWorkScopeAccessor _scope;
    private readonly EntityFixture _testEntity = new() { Id = 1 };

    public EFRepoTests()
    {
        _dbName = new Fixture().Create<string>();
        _dbContext = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options);
        _scope = new DefaultUnitOfWorkScopeAccessor();

        _efRepo = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            _dbContext,
            _scope);
    }

    protected override IRepo<EntityFixture> Repo => _efRepo;

    [Fact]
    public async Task CreateAsync_AddsEntity()
    {
        await _efRepo.CreateAsync(_testEntity);
        var entity = _dbContext.Set<EntityFixture>().Find(_testEntity.Id);
        Assert.Equal(_testEntity, entity);
    }

    [Fact]
    public async Task GetAsync_ReturnsEntity()
    {
        await _efRepo.CreateAsync(_testEntity);
        var entity = await Repo.GetAsync(e => e.Id == _testEntity.Id);
        Assert.Equal(_testEntity, entity);
    }

    [Fact]
    public async Task UpdateAsync_AttachesUntrackedEntity()
    {
        await _efRepo.CreateAsync(_testEntity);
        _dbContext.Set<EntityFixture>().Entry(_testEntity).State = EntityState.Detached;

        var entity = new EntityFixture { Id = _testEntity.Id };
        await _efRepo.UpdateAsync(entity);

        var updatedEntity = _dbContext.Set<EntityFixture>().Find(entity.Id);
        Assert.NotNull(updatedEntity);
        Assert.NotEqual(_testEntity, updatedEntity);
        Assert.Equal(entity, updatedEntity);
        Assert.NotNull(updatedEntity.ModifiedUtc);
        Assert.InRange(updatedEntity.ModifiedUtc.Value, DateTime.UtcNow.AddSeconds(-2), DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingEntity()
    {
        await _efRepo.CreateAsync(_testEntity);
        var untrackedSetupEntity = new EntityFixture { Id = _testEntity.Id };
        await _efRepo.UpdateAsync(untrackedSetupEntity);
        Assert.NotNull(_testEntity.ModifiedUtc);
        Assert.InRange(_testEntity.ModifiedUtc.Value, DateTime.UtcNow.AddSeconds(-2), DateTime.UtcNow);
    }

    [Fact]
    public async Task DeferredPersistence_InsideUnitOfWork_DoesNotSaveUntilCommit()
    {
        var uow = new EFUoWProvider(_dbContext, _scope);
        await uow.BeginAsync();

        var e1 = new EntityFixture { Id = 200 };
        var e2 = new EntityFixture { Id = 201 };
        await _efRepo.CreateAsync(e1);
        await _efRepo.CreateAsync(e2);

        using var readContext = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>()
                .UseInMemoryDatabase(_dbName)
                .Options);
        Assert.Null(readContext.Set<EntityFixture>().Find(200));
        Assert.Null(readContext.Set<EntityFixture>().Find(201));

        await uow.CommitAsync();

        Assert.NotNull(readContext.Set<EntityFixture>().Find(200));
        Assert.NotNull(readContext.Set<EntityFixture>().Find(201));
    }

    [Fact]
    public async Task Rollback_ClearsPendingChanges_ForDeferredOps()
    {
        var uow = new EFUoWProvider(_dbContext, _scope);
        await uow.BeginAsync();

        var e1 = new EntityFixture { Id = 300 };
        await _efRepo.CreateAsync(e1);

        using var readContext = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>()
                .UseInMemoryDatabase(_dbName)
                .Options);
        Assert.Null(readContext.Set<EntityFixture>().Find(300));

        await uow.RollbackAsync();

        Assert.Null(readContext.Set<EntityFixture>().Find(300));
        Assert.DoesNotContain(_dbContext.ChangeTracker.Entries(), e => e.Entity is EntityFixture ef && ef.Id == 300);
    }

    protected override int FillRepoAndReturnId()
    {
        _dbContext.Set<EntityFixture>().AddRange(Fixture.CreateMany<EntityFixture>(5));
        _dbContext.SaveChanges();
        return _dbContext.Set<EntityFixture>().Skip(1).First().Id;
    }
}
