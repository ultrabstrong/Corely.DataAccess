using System.Data.Common;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.EntityFramework.UnitOfWork;

public partial class EFUoWProviderTests
{
    private static (ServiceProvider sp, EFUoWProvider uow) BuildUoW()
    {
        var services = new ServiceCollection();
        services.AddScoped<EFUoWProvider>();
        var sp = services.BuildServiceProvider();
        var uow = sp.GetRequiredService<EFUoWProvider>();
        return (sp, uow);
    }

    private sealed class CountingTransactionInterceptor : DbTransactionInterceptor
    {
        private int _beginCount;
        private int _commitCount;
        private int _rollbackCount;
        public int BeginCount => _beginCount;
        public int CommitCount => _commitCount;
        public int RollbackCount => _rollbackCount;

        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result
        )
        {
            Interlocked.Increment(ref _beginCount);
            return base.TransactionStarting(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _beginCount);
            return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(
            DbTransaction transaction,
            TransactionEndEventData eventData
        )
        {
            Interlocked.Increment(ref _commitCount);
            base.TransactionCommitted(transaction, eventData);
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _commitCount);
            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(
            DbTransaction transaction,
            TransactionEndEventData eventData
        )
        {
            Interlocked.Increment(ref _rollbackCount);
            base.TransactionRolledBack(transaction, eventData);
        }

        public override Task TransactionRolledBackAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _rollbackCount);
            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }
    }

    [Fact]
    public async Task BeginAsync_WhenAlreadyActive_Throws()
    {
        var (_, uow) = BuildUoW();

        await uow.BeginAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginAsync());
    }

    [Fact]
    public async Task BeginAsync_SetsActiveFlag()
    {
        var (_, uow) = BuildUoW();

        await uow.BeginAsync();

        Assert.True(uow.IsActive);
    }

    [Fact]
    public async Task BeginAsync_StartsTransaction_ForEachRegisteredContext()
    {
        var interceptor = new CountingTransactionInterceptor();

        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx1 = new DbContextFixture(options1);

        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx2 = new AnotherDbContextFixture(options2);

        var (_, uow) = BuildUoW();

        uow.Register(ctx1);
        uow.Register(ctx2);

        await uow.BeginAsync();

        Assert.Equal(2, interceptor.BeginCount);
    }

    [Fact]
    public async Task CommitAsync_WhenNoActiveScope_Throws()
    {
        var (_, uow) = BuildUoW();
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task CommitAsync_SavesChanges_ForEachRegisteredContext_AndDeactivates()
    {
        // Use file-based SQLite to validate pre-commit invisibility across connections
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        try
        {
            var options1 = new DbContextOptionsBuilder<DbContextFixture>()
                .UseSqlite($"Data Source={file1}")
                .Options;
            var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
                .UseSqlite($"Data Source={file2}")
                .Options;
            var ctx1 = new DbContextFixture(options1);
            var ctx2 = new AnotherDbContextFixture(options2);

            // Create schema
            ctx1.Database.EnsureCreated();
            ctx2.Database.EnsureCreated();

            var logger1 = Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>();
            var logger2 = Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>();

            var (sp, uow) = BuildUoW();

            var repo1 = new EFRepo<DbContextFixture, EntityFixture>(logger1, ctx1, uow);
            var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(logger2, ctx2, uow);

            uow.Register(ctx1);
            uow.Register(ctx2);

            await uow.BeginAsync();

            await repo1.CreateAsync(new EntityFixture { Id = 10 });
            await repo2.CreateAsync(new EntityFixture { Id = 20 });

            using (
                var read1 = new DbContextFixture(
                    new DbContextOptionsBuilder<DbContextFixture>()
                        .UseSqlite($"Data Source={file1}")
                        .Options
                )
            )
            {
                Assert.Null(read1.Set<EntityFixture>().Find(10));
            }
            using (
                var read2 = new AnotherDbContextFixture(
                    new DbContextOptionsBuilder<AnotherDbContextFixture>()
                        .UseSqlite($"Data Source={file2}")
                        .Options
                )
            )
            {
                Assert.Null(read2.Set<EntityFixture>().Find(20));
            }

            await uow.CommitAsync();

            using (
                var read1 = new DbContextFixture(
                    new DbContextOptionsBuilder<DbContextFixture>()
                        .UseSqlite($"Data Source={file1}")
                        .Options
                )
            )
            {
                Assert.NotNull(read1.Set<EntityFixture>().Find(10));
            }
            using (
                var read2 = new AnotherDbContextFixture(
                    new DbContextOptionsBuilder<AnotherDbContextFixture>()
                        .UseSqlite($"Data Source={file2}")
                        .Options
                )
            )
            {
                Assert.NotNull(read2.Set<EntityFixture>().Find(20));
            }

            Assert.False(uow.IsActive);
            sp.Dispose();
        }
        finally
        {
            try
            {
                File.Delete(file1);
            }
            catch { }
            try
            {
                File.Delete(file2);
            }
            catch { }
        }
    }

    [Fact]
    public async Task CommitAsync_CommitsTransactions_ForRelationalProviders()
    {
        var interceptor = new CountingTransactionInterceptor();

        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx1 = new DbContextFixture(options1);

        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx2 = new AnotherDbContextFixture(options2);

        var (_, uow) = BuildUoW();

        uow.Register(ctx1);
        uow.Register(ctx2);

        await uow.BeginAsync();
        await uow.CommitAsync();

        Assert.Equal(2, interceptor.BeginCount);
        Assert.Equal(2, interceptor.CommitCount);
    }

    [Fact]
    public async Task RollbackAsync_WhenNoActiveScope_Throws()
    {
        var (_, uow) = BuildUoW();
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackAsync());
    }

    [Fact]
    public async Task RollbackAsync_ClearsTrackedChanges_ForEachContext_AndDeactivates()
    {
        // Use file-based SQLite so that SaveChanges within an active UoW happen inside a transaction
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        try
        {
            var options1 = new DbContextOptionsBuilder<DbContextFixture>()
                .UseSqlite($"Data Source={file1}")
                .Options;
            var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
                .UseSqlite($"Data Source={file2}")
                .Options;
            var ctx1 = new DbContextFixture(options1);
            var ctx2 = new AnotherDbContextFixture(options2);

            // Create schema
            ctx1.Database.EnsureCreated();
            ctx2.Database.EnsureCreated();

            var (_, uow) = BuildUoW();

            var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
                Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
                ctx1,
                uow
            );
            var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
                Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
                ctx2,
                uow
            );

            uow.Register(ctx1);
            uow.Register(ctx2);

            await uow.BeginAsync();

            await repo1.CreateAsync(new EntityFixture { Id = 101 });
            await repo2.CreateAsync(new EntityFixture { Id = 202 });

            using (
                var read1 = new DbContextFixture(
                    new DbContextOptionsBuilder<DbContextFixture>()
                        .UseSqlite($"Data Source={file1}")
                        .Options
                )
            )
            {
                Assert.Null(read1.Set<EntityFixture>().Find(101));
            }
            using (
                var read2 = new AnotherDbContextFixture(
                    new DbContextOptionsBuilder<AnotherDbContextFixture>()
                        .UseSqlite($"Data Source={file2}")
                        .Options
                )
            )
            {
                Assert.Null(read2.Set<EntityFixture>().Find(202));
            }

            await uow.RollbackAsync();

            using (
                var read1 = new DbContextFixture(
                    new DbContextOptionsBuilder<DbContextFixture>()
                        .UseSqlite($"Data Source={file1}")
                        .Options
                )
            )
            {
                Assert.Null(read1.Set<EntityFixture>().Find(101));
            }
            using (
                var read2 = new AnotherDbContextFixture(
                    new DbContextOptionsBuilder<AnotherDbContextFixture>()
                        .UseSqlite($"Data Source={file2}")
                        .Options
                )
            )
            {
                Assert.Null(read2.Set<EntityFixture>().Find(202));
            }

            Assert.False(uow.IsActive);
        }
        finally
        {
            try
            {
                File.Delete(file1);
            }
            catch { }
            try
            {
                File.Delete(file2);
            }
            catch { }
        }
    }

    [Fact]
    public async Task LateEnlistment_Relational_StartedAndRolledBack()
    {
        var interceptor = new CountingTransactionInterceptor();

        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx1 = new DbContextFixture(options1);

        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;
        var ctx2 = new AnotherDbContextFixture(options2);

        var (_, uow) = BuildUoW();

        await uow.BeginAsync();
        uow.Register(ctx1);
        Assert.Equal(1, interceptor.BeginCount);

        uow.Register(ctx2);
        Assert.Equal(2, interceptor.BeginCount);

        await uow.RollbackAsync();
        Assert.Equal(2, interceptor.RollbackCount);
    }

    private sealed class CI : DbTransactionInterceptor
    {
        public int BeginCount { get; private set; }

        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection c,
            TransactionStartingEventData e,
            InterceptionResult<DbTransaction> r
        )
        {
            BeginCount++;
            return base.TransactionStarting(c, e, r);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection c,
            TransactionStartingEventData e,
            InterceptionResult<DbTransaction> r,
            CancellationToken t = default
        )
        {
            BeginCount++;
            return base.TransactionStartingAsync(c, e, r, t);
        }
    }

    [Fact]
    public async Task Register_Duplicate_DuringActive_DoesNotStartSecondTransaction()
    {
        var ci = new CI();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(ci)
            .Options;
        var ctx = new DbContextFixture(options);

        var (_, uow) = BuildUoW();
        uow.Register(ctx);
        await uow.BeginAsync();
        Assert.Equal(1, ci.BeginCount);

        // Duplicate register should not open a second tx
        uow.Register(ctx);
        Assert.Equal(1, ci.BeginCount);
    }

    [Fact]
    public async Task Register_Null_DoesNothing()
    {
        var (_, uow) = BuildUoW();
        await uow.BeginAsync();
        uow.Register(null!);
        // No exception, nothing to assert beyond no-throw
        await uow.RollbackAsync();
    }

    [Fact]
    public async Task Dispose_ClearsState_AndAllowsNewRegistrations()
    {
        var ci = new CI();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(ci)
            .Options;
        var ctx = new DbContextFixture(options);

        var (_, uow) = BuildUoW();
        uow.Register(ctx);
        await uow.BeginAsync();
        Assert.Equal(1, ci.BeginCount);

        uow.Dispose();
        Assert.False(uow.IsActive);

        // Re-register and begin again should start a new tx
        uow.Register(ctx);
        await uow.BeginAsync();
        Assert.Equal(2, ci.BeginCount);
        await uow.RollbackAsync();
    }

    [Fact]
    public async Task DisposeAsync_ClearsState_AndAllowsNewRegistrations()
    {
        var ci = new CI();
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(ci)
            .Options;
        var ctx = new DbContextFixture(options);

        var (_, uow) = BuildUoW();
        uow.Register(ctx);
        await uow.BeginAsync();
        Assert.Equal(1, ci.BeginCount);

        await uow.DisposeAsync();
        Assert.False(uow.IsActive);

        uow.Register(ctx);
        await uow.BeginAsync();
        Assert.Equal(2, ci.BeginCount);
        await uow.RollbackAsync();
    }

    [Fact]
    public async Task Commit_OnlySavesContextsWithChanges()
    {
        // Two in-memory contexts
        var db1 = Guid.NewGuid().ToString();
        var db2 = Guid.NewGuid().ToString();
        var ctx1 = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(db1).Options
        );
        var ctx2 = new AnotherDbContextFixture(
            new DbContextOptionsBuilder<AnotherDbContextFixture>().UseInMemoryDatabase(db2).Options
        );

        var (_, uow) = BuildUoW();

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1,
            uow
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2,
            uow
        );

        uow.Register(ctx1);
        uow.Register(ctx2);

        await uow.BeginAsync();

        // Make changes only in ctx1
        await repo1.CreateAsync(new EntityFixture { Id = 777 });

        await uow.CommitAsync();

        using (
            var r1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(db1).Options
            )
        )
        {
            Assert.NotNull(r1.Set<EntityFixture>().Find(777));
        }
        using (
            var r2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(db2)
                    .Options
            )
        )
        {
            Assert.Empty(r2.Set<EntityFixture>().ToList());
        }
    }
}
