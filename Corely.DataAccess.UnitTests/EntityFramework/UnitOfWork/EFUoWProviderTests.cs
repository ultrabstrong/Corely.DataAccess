using System.Data.Common;
using System.Reflection;
using Corely.DataAccess.EntityFramework.Repos;
using Corely.DataAccess.EntityFramework.UnitOfWork;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.EntityFramework.UnitOfWork;

public class EFUoWProviderTests
{
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
    public async Task BeginAsync_WhenScopeAlreadyActive_Throws()
    {
        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        await uow.BeginAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginAsync());
    }

    [Fact]
    public async Task BeginAsync_SetsScopeActive()
    {
        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        await uow.BeginAsync();

        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(scopeField);

        var scope = scopeField!.GetValue(uow);
        Assert.NotNull(scope);

        var isActiveProp = scope!
            .GetType()
            .GetProperty(
                "IsActive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        Assert.NotNull(isActiveProp);

        var isActive = (bool)(isActiveProp!.GetValue(scope) ?? false);
        Assert.True(isActive);
    }

    [Fact]
    public async Task BeginAsync_StartsTransaction_ForEachRegisteredContext()
    {
        var interceptor = new CountingTransactionInterceptor();

        // Build two relational contexts (SQLite) so transactions are supported
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

        // Prepare repos bound to each context and register them into the same scope
        var logger1 = Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>();
        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(logger1, ctx1);

        var logger2 = Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>();
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(logger2, ctx2);

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        // Reflect the private scope instance and register both contexts
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(scopeField);
        var scope = (EFUnitOfWorkScope)scopeField!.GetValue(uow)!;

        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();

        // Expect one transaction begin per registered context (2)
        Assert.Equal(2, interceptor.BeginCount);
    }

    [Fact]
    public async Task CommitAsync_WhenNoActiveScope_Throws()
    {
        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task CommitAsync_SavesChanges_ForEachRegisteredContext_AndDeactivatesScope()
    {
        // InMemory provider: no transactions, but SaveChanges should be called
        var dbName1 = Guid.NewGuid().ToString();
        var dbName2 = Guid.NewGuid().ToString();
        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(dbName1)
            .Options;
        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseInMemoryDatabase(dbName2)
            .Options;
        var ctx1 = new DbContextFixture(options1);
        var ctx2 = new AnotherDbContextFixture(options2);

        var logger1 = Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>();
        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(logger1, ctx1);
        var logger2 = Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>();
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(logger2, ctx2);

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        // Register contexts into the same scope
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(scopeField);
        var scope = (EFUnitOfWorkScope)scopeField!.GetValue(uow)!;

        repo1.SetScope(scope);
        repo2.SetScope(scope);

        // Begin UoW so repo operations defer SaveChanges
        await uow.BeginAsync();

        await repo1.CreateAsync(new EntityFixture { Id = 10 });
        await repo2.CreateAsync(new EntityFixture { Id = 20 });

        // Confirm not yet persisted in separate read contexts
        using (
            var read1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
        {
            Assert.Null(read1.Set<EntityFixture>().Find(10));
        }
        using (
            var read2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
        {
            Assert.Null(read2.Set<EntityFixture>().Find(20));
        }

        await uow.CommitAsync();

        // Verify persisted now
        using (
            var read1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
        {
            Assert.NotNull(read1.Set<EntityFixture>().Find(10));
        }
        using (
            var read2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
        {
            Assert.NotNull(read2.Set<EntityFixture>().Find(20));
        }

        // Scope should be inactive after commit
        var isActiveProp = scope!
            .GetType()
            .GetProperty(
                "IsActive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        Assert.NotNull(isActiveProp);
        var isActive = (bool)(isActiveProp!.GetValue(scope) ?? false);
        Assert.False(isActive);
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

        var logger1 = Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>();
        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(logger1, ctx1);
        var logger2 = Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>();
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(logger2, ctx2);

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(scopeField);
        var scope = (EFUnitOfWorkScope)scopeField!.GetValue(uow)!;

        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();
        await uow.CommitAsync();

        Assert.Equal(2, interceptor.BeginCount);
        Assert.Equal(2, interceptor.CommitCount);
    }

    [Fact]
    public async Task RollbackAsync_WhenNoActiveScope_Throws()
    {
        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackAsync());
    }

    [Fact]
    public async Task RollbackAsync_ClearsTrackedChanges_ForEachContext_AndDeactivatesScope()
    {
        // Two InMemory contexts (no transaction), but tracked changes must be cleared
        var dbName1 = Guid.NewGuid().ToString();
        var dbName2 = Guid.NewGuid().ToString();
        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(dbName1)
            .Options;
        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseInMemoryDatabase(dbName2)
            .Options;
        var ctx1 = new DbContextFixture(options1);
        var ctx2 = new AnotherDbContextFixture(options2);

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(scopeField);
        var scope = (EFUnitOfWorkScope)scopeField!.GetValue(uow)!;

        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();

        await repo1.CreateAsync(new EntityFixture { Id = 101 });
        await repo2.CreateAsync(new EntityFixture { Id = 202 });

        // Ensure not yet persisted
        using (
            var read1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
        {
            Assert.Null(read1.Set<EntityFixture>().Find(101));
        }
        using (
            var read2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
        {
            Assert.Null(read2.Set<EntityFixture>().Find(202));
        }

        await uow.RollbackAsync();

        // Still not persisted and trackers cleared
        using (
            var read1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
        {
            Assert.Null(read1.Set<EntityFixture>().Find(101));
        }
        using (
            var read2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
        {
            Assert.Null(read2.Set<EntityFixture>().Find(202));
        }

        Assert.Empty(ctx1.ChangeTracker.Entries());
        Assert.Empty(ctx2.ChangeTracker.Entries());

        var isActiveProp = scope!
            .GetType()
            .GetProperty(
                "IsActive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        Assert.NotNull(isActiveProp);
        var isActive = (bool)(isActiveProp!.GetValue(scope) ?? false);
        Assert.False(isActive);
    }

    [Fact]
    public async Task RollbackAsync_RollsBackTransactions_ForRelationalProviders()
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

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());

        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(scopeField);
        var scope = (EFUnitOfWorkScope)scopeField!.GetValue(uow)!;

        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();
        await uow.RollbackAsync();

        Assert.Equal(2, interceptor.BeginCount);
        Assert.Equal(2, interceptor.RollbackCount);
    }

    [Fact]
    public async Task Dispose_ClearsTransactions_AndDeactivatesScope()
    {
        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var ctx1 = new DbContextFixture(options1);

        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var ctx2 = new AnotherDbContextFixture(options2);

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var txField = typeof(EFUoWProvider).GetField(
            "_transactions",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        var scope = (EFUnitOfWorkScope)scopeField.GetValue(uow)!;
        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();

        var txDict = txField.GetValue(uow)!;
        var countProp = txDict.GetType().GetProperty("Count")!;
        var beforeCount = (int)countProp.GetValue(txDict)!;
        Assert.True(beforeCount >= 2);

        uow.Dispose();

        var txDictAfter = txField.GetValue(uow)!;
        var afterCount = (int)countProp.GetValue(txDictAfter)!;
        Assert.Equal(0, afterCount);

        var isActiveProp = scope
            .GetType()
            .GetProperty(
                "IsActive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )!;
        var isActive = (bool)(isActiveProp.GetValue(scope) ?? false);
        Assert.False(isActive);
    }

    [Fact]
    public async Task DisposeAsync_ClearsTransactions_AndDeactivatesScope()
    {
        var options1 = new DbContextOptionsBuilder<DbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var ctx1 = new DbContextFixture(options1);

        var options2 = new DbContextOptionsBuilder<AnotherDbContextFixture>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var ctx2 = new AnotherDbContextFixture(options2);

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var txField = typeof(EFUoWProvider).GetField(
            "_transactions",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        var scope = (EFUnitOfWorkScope)scopeField.GetValue(uow)!;
        repo1.SetScope(scope);
        repo2.SetScope(scope);

        await uow.BeginAsync();

        var txDict = txField.GetValue(uow)!;
        var countProp = txDict.GetType().GetProperty("Count")!;
        var beforeCount = (int)countProp.GetValue(txDict)!;
        Assert.True(beforeCount >= 2);

        await uow.DisposeAsync();

        var txDictAfter = txField.GetValue(uow)!;
        var afterCount = (int)countProp.GetValue(txDictAfter)!;
        Assert.Equal(0, afterCount);

        var isActiveProp = scope
            .GetType()
            .GetProperty(
                "IsActive",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )!;
        var isActive = (bool)(isActiveProp.GetValue(scope) ?? false);
        Assert.False(isActive);
    }

    [Fact]
    public async Task LateEnlistment_Relational_TransactionStarted_AndCommitted()
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

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var scope = (EFUnitOfWorkScope)scopeField.GetValue(uow)!;

        // Register first context then begin
        repo1.SetScope(scope);
        await uow.BeginAsync();
        Assert.Equal(1, interceptor.BeginCount);

        // Late register second context; expect a new transaction to start
        repo2.SetScope(scope);

        // Allow async handler to run
        for (var i = 0; i < 50 && interceptor.BeginCount < 2; i++)
            await Task.Delay(10);
        Assert.Equal(2, interceptor.BeginCount);

        await uow.CommitAsync();
        Assert.Equal(2, interceptor.CommitCount);
    }

    [Fact]
    public async Task LateEnlistment_InMemory_SavesChanges_NoTransactions()
    {
        var dbName1 = Guid.NewGuid().ToString();
        var dbName2 = Guid.NewGuid().ToString();
        var ctx1 = new DbContextFixture(
            new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
        );
        var ctx2 = new AnotherDbContextFixture(
            new DbContextOptionsBuilder<AnotherDbContextFixture>()
                .UseInMemoryDatabase(dbName2)
                .Options
        );

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var txField = typeof(EFUoWProvider).GetField(
            "_transactions",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var scope = (EFUnitOfWorkScope)scopeField.GetValue(uow)!;

        repo1.SetScope(scope);
        await uow.BeginAsync();

        // Late register second
        repo2.SetScope(scope);

        await repo1.CreateAsync(new EntityFixture { Id = 501 });
        await repo2.CreateAsync(new EntityFixture { Id = 502 });

        // Not yet persisted
        using (
            var r1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
            Assert.Null(r1.Set<EntityFixture>().Find(501));
        using (
            var r2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
            Assert.Null(r2.Set<EntityFixture>().Find(502));

        await uow.CommitAsync();

        // Persisted now
        using (
            var r1 = new DbContextFixture(
                new DbContextOptionsBuilder<DbContextFixture>().UseInMemoryDatabase(dbName1).Options
            )
        )
            Assert.NotNull(r1.Set<EntityFixture>().Find(501));
        using (
            var r2 = new AnotherDbContextFixture(
                new DbContextOptionsBuilder<AnotherDbContextFixture>()
                    .UseInMemoryDatabase(dbName2)
                    .Options
            )
        )
            Assert.NotNull(r2.Set<EntityFixture>().Find(502));

        // Transactions dictionary should contain entries but null for InMemory providers
        var txDict = (System.Collections.IDictionary)txField.GetValue(uow)!;
        Assert.NotNull(txDict);
        // After commit it is cleared; but scope deactivates and clears in finally
        // So we can only assert that commit succeeded without any transaction interceptor events, which we didn't register here.
    }

    [Fact]
    public async Task LateEnlistment_Relational_RolledBack()
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

        var repo1 = new EFRepo<DbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<DbContextFixture, EntityFixture>>>(),
            ctx1
        );
        var repo2 = new EFRepo<AnotherDbContextFixture, EntityFixture>(
            Moq.Mock.Of<ILogger<EFRepo<AnotherDbContextFixture, EntityFixture>>>(),
            ctx2
        );

        var uow = new EFUoWProvider(new ServiceCollection().BuildServiceProvider());
        var scopeField = typeof(EFUoWProvider).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var scope = (EFUnitOfWorkScope)scopeField.GetValue(uow)!;

        repo1.SetScope(scope);
        await uow.BeginAsync();
        Assert.Equal(1, interceptor.BeginCount);

        repo2.SetScope(scope);
        for (var i = 0; i < 50 && interceptor.BeginCount < 2; i++)
            await Task.Delay(10);
        Assert.Equal(2, interceptor.BeginCount);

        await uow.RollbackAsync();
        Assert.Equal(2, interceptor.RollbackCount);
    }
}
