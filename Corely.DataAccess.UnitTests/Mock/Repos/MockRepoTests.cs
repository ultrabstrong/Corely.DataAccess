using AutoFixture;
using Corely.DataAccess.Interfaces.Entities;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Mock.Repos;
using Corely.DataAccess.UnitTests.Fixtures;

namespace Corely.DataAccess.UnitTests.Mock.Repos;

public class MockRepoTests : RepoTestsBase
{
    private readonly MockRepo<EntityFixture> _mockRepo = new();
    protected override IRepo<EntityFixture> Repo => _mockRepo;

    protected override int FillRepoAndReturnId()
    {
        var entityList = Fixture.CreateMany<EntityFixture>(5).ToList();
        _mockRepo.CreateAsync(entityList); // fire & forget acceptable in tests
        return entityList[2].Id;
    }

    private EntityFixture NewEntity(
        int id,
        DateTime? createdUtc = null,
        DateTime? modifiedUtc = null
    ) =>
        new()
        {
            Id = id,
            CreatedUtc = createdUtc ?? default,
            ModifiedUtc = modifiedUtc,
        };

    [Fact]
    public async Task Create_SetsCreatedUtc_WhenUnset()
    {
        var e = NewEntity(100);
        Assert.Equal(default, e.CreatedUtc);
        await _mockRepo.CreateAsync(e);
        Assert.NotEqual(default, e.CreatedUtc);
    }

    [Fact]
    public async Task Create_DoesNotOverrideCreatedUtc_WhenPreset()
    {
        var preset = DateTime.UtcNow.AddDays(-1).AddMilliseconds(-DateTime.UtcNow.Millisecond);
        var e = NewEntity(101, preset);
        await _mockRepo.CreateAsync(e);
        Assert.Equal(preset, e.CreatedUtc);
    }

    [Fact]
    public async Task Update_ByKey_ReplacesEntity_WhenDifferentReference()
    {
        var original = NewEntity(102, DateTime.UtcNow.AddDays(-2));
        await _mockRepo.CreateAsync(original);

        var replacement = NewEntity(102, original.CreatedUtc);
        await _mockRepo.UpdateAsync(replacement);

        var fetched = await _mockRepo.GetAsync(x => x.Id == 102);

        Assert.NotNull(fetched);
        Assert.Same(replacement, fetched);
        Assert.NotSame(original, fetched);

        var count = await _mockRepo.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Update_PreservesOriginalCreatedUtc_WhenIncomingUnset()
    {
        var originalCreated = DateTime.UtcNow.AddDays(-3);
        var original = NewEntity(103, originalCreated);
        await _mockRepo.CreateAsync(original);

        var incoming = NewEntity(103);
        await _mockRepo.UpdateAsync(incoming);

        var fetched = await _mockRepo.GetAsync(x => x.Id == 103);

        Assert.NotNull(fetched);
        Assert.Equal(originalCreated, fetched!.CreatedUtc);
    }

    [Fact]
    public async Task Update_SetsModifiedUtc()
    {
        var original = NewEntity(104, DateTime.UtcNow.AddDays(-1));
        await _mockRepo.CreateAsync(original);

        var incoming = NewEntity(104, original.CreatedUtc);
        var before = DateTime.UtcNow;
        await _mockRepo.UpdateAsync(incoming);

        Assert.NotNull(incoming.ModifiedUtc);
        Assert.True(incoming.ModifiedUtc > before.AddSeconds(-1));
    }

    [Fact]
    public async Task Delete_ByKey_RemovesMatchingEntity_WhenDifferentReference()
    {
        var original = NewEntity(105, DateTime.UtcNow.AddDays(-1));
        await _mockRepo.CreateAsync(original);

        var phantom = NewEntity(105);
        await _mockRepo.DeleteAsync(phantom);

        var count = await _mockRepo.CountAsync();
        Assert.Equal(0, count);
    }

    private class NoKeyEntity : IHasCreatedUtc
    {
        public DateTime CreatedUtc { get; set; }
        public string? Value { get; set; }
    }

    [Fact]
    public async Task Delete_FallbacksToReference_WhenNoKeyInterface()
    {
        var repo = new MockRepo<NoKeyEntity>();
        await repo.CreateAsync(new[] { new NoKeyEntity(), new NoKeyEntity() });

        var first = repo.Entities.First();
        var second = repo.Entities.Skip(1).First();

        await repo.DeleteAsync(first);

        Assert.Single(repo.Entities);
        Assert.Same(second, repo.Entities.First());
    }

    [Fact]
    public async Task Update_FallbacksToReference_WhenNoKeyInterface()
    {
        var repo = new MockRepo<NoKeyEntity>();
        var a = new NoKeyEntity();
        var b = new NoKeyEntity();
        await repo.CreateAsync([a, b]);

        var c = new NoKeyEntity();
        await repo.UpdateAsync(c);

        Assert.Equal(2, repo.Entities.Count);
        Assert.Contains(a, repo.Entities);
        Assert.Contains(b, repo.Entities);
    }

    [Fact]
    public async Task Create_Range_SetsCreatedUtc_ForEach()
    {
        var e1 = NewEntity(106);
        var e2 = NewEntity(107);

        await _mockRepo.CreateAsync([e1, e2]);

        Assert.NotEqual(default, e1.CreatedUtc);
        Assert.NotEqual(default, e2.CreatedUtc);
    }

    [Fact]
    public async Task Update_DoesNotChangeCreatedUtc_WhenIncomingAlreadyHasValue()
    {
        var original = NewEntity(108, DateTime.UtcNow.AddDays(-5));
        await _mockRepo.CreateAsync(original);

        var newCreated = DateTime.UtcNow.AddDays(-1);
        var incoming = NewEntity(108, newCreated);
        await _mockRepo.UpdateAsync(incoming);

        var fetched = await _mockRepo.GetAsync(x => x.Id == 108);

        Assert.NotNull(fetched);
        Assert.Equal(newCreated, fetched!.CreatedUtc);
    }
}
