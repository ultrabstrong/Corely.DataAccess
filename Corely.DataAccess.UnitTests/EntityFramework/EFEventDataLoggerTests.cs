using System.Collections.Concurrent;
using Corely.DataAccess.EntityFramework;
using Corely.DataAccess.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class EFEventDataLoggerTests
{
    private class TestLogger : ILogger
    {
        private readonly ConcurrentQueue<(
            LogLevel Level,
            EventId EventId,
            Exception? Ex,
            string Message,
            object?[] Args
        )> _entries = new();
        private readonly ConcurrentStack<IDisposable> _scopes = new();

        public IEnumerable<(
            LogLevel Level,
            EventId EventId,
            Exception? Ex,
            string Message,
            object?[] Args
        )> Entries => _entries;

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            var d = new Scope();
            _scopes.Push(d);
            return d;
        }

        public virtual bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var msg = formatter(state, exception);
            var args = ExtractArgs(state);
            _entries.Enqueue((logLevel, eventId, exception, msg, args));
        }

        private static object?[] ExtractArgs<TState>(TState state)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
            {
                var list = kvps.Where(kv => kv.Key != "{OriginalFormat}")
                    .Select(kv => kv.Value)
                    .ToArray();
                return list;
            }
            return [];
        }

        protected sealed class Scope : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class FilterLogger : TestLogger
    {
        private readonly Func<LogLevel, bool> _isEnabled;

        public FilterLogger(Func<LogLevel, bool> isEnabled) => _isEnabled = isEnabled;

        public override bool IsEnabled(LogLevel logLevel) => _isEnabled(logLevel);
    }

    private sealed class TestLoggingOptions : ILoggingOptions
    {
        public bool IsSensitiveDataLoggingEnabled => false;
        public bool IsSensitiveDataLoggingWarned { get; set; }

        public bool ShouldWarnForStringEnumValueInJson(Type type) => false;

        public bool DetailedErrorsEnabled => false;

        // Provide a dummy value; not used by our tests
        public WarningsConfiguration WarningsConfiguration => default!;

        public void Initialize(IDbContextOptions options) { }

        public void Validate(IDbContextOptions options) { }
    }

    private sealed class TestEventDefinition : EventDefinitionBase
    {
        public TestEventDefinition(EventId id, LogLevel level)
            : base(new TestLoggingOptions(), id, level, "TestEvent") { }
    }

    private static DbContextFixture CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbContextFixture>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbContextFixture(options);
    }

    private static DbContextEventData CreateDbContextEvent(
        EventId id,
        LogLevel level,
        DbContext ctx
    )
    {
        var def = new TestEventDefinition(id, level);
        return new DbContextEventData(def, static (d, e) => $"{d.EventId}:{e.GetType().Name}", ctx);
    }

    private static EventData CreateBaseEvent(EventId id, LogLevel level)
    {
        var def = new TestEventDefinition(id, level);
        return new EventData(def, static (d, e) => $"{d.EventId}:{e.GetType().Name}");
    }

    [Fact]
    public void Write_Throws_When_Logger_Null()
    {
        ILogger? logger = null;
        var ev = CreateBaseEvent(new EventId(999, "NullLogger"), LogLevel.Information);
        var ex = Assert.Throws<ArgumentNullException>(() => EFEventDataLogger.Write(logger!, ev));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Write_Throws_When_EventData_Null()
    {
        var logger = new TestLogger();
        EventData? ev = null;
        var ex = Assert.Throws<ArgumentNullException>(() => EFEventDataLogger.Write(logger, ev!));
        Assert.Equal("eventData", ex.ParamName);
    }

    [Fact]
    public void Write_OnlyExecutedCommandsTrue_Skips_DbContextEvent()
    {
        var logger = new TestLogger();
        using var ctx = CreateContext();
        var ev = CreateDbContextEvent(
            new EventId(123, "TestDbContextEvent"),
            LogLevel.Information,
            ctx
        );

        EFEventDataLogger.Write(logger, ev, onlyLogExecutedCommands: true);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Write_DefaultOnlyExecutedCommandsTrue_Skips_DbContextEvent()
    {
        var logger = new TestLogger();
        using var ctx = CreateContext();
        var ev = CreateDbContextEvent(new EventId(124, "DefaultSkip"), LogLevel.Information, ctx);

        // onlyLogExecutedCommands defaults to true
        EFEventDataLogger.Write(logger, ev);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Write_OnlyExecutedCommandsFalse_Logs_DbContextEvent_WithEventId()
    {
        var logger = new TestLogger();
        using var ctx = CreateContext();
        var evId = new EventId(456, "AnotherEvent");
        var ev = CreateDbContextEvent(evId, LogLevel.Warning, ctx);

        EFEventDataLogger.Write(logger, ev, onlyLogExecutedCommands: false);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(evId, entry.EventId);
        Assert.Contains("EF event", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, entry.Args.Length);
        Assert.Equal(ev.GetType().Name, entry.Args[0]);
        Assert.Equal(ctx.GetType().Name, entry.Args[1]);
    }

    [Fact]
    public void Write_InfoOverride_Debug_ChangesLevel()
    {
        var logger = new TestLogger();
        using var ctx = CreateContext();
        var ev = CreateDbContextEvent(new EventId(457, "InfoToDebug"), LogLevel.Information, ctx);

        EFEventDataLogger.Write(
            logger,
            ev,
            writeInfoLogsAs: EFEventDataLogger.WriteInfoLogsAs.Debug,
            onlyLogExecutedCommands: false
        );

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
    }

    [Fact]
    public void Write_InfoOverride_Trace_ChangesLevel()
    {
        var logger = new TestLogger();
        using var ctx = CreateContext();
        var ev = CreateDbContextEvent(new EventId(458, "InfoToTrace"), LogLevel.Information, ctx);

        EFEventDataLogger.Write(
            logger,
            ev,
            writeInfoLogsAs: EFEventDataLogger.WriteInfoLogsAs.Trace,
            onlyLogExecutedCommands: false
        );

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Trace, entry.Level);
    }

    [Fact]
    public void Write_IsEnabledFalse_ShortCircuits()
    {
        // Event effective level becomes Debug via override; logger disables Debug
        var logger = new FilterLogger(lvl => lvl != LogLevel.Debug);
        using var ctx = CreateContext();
        var ev = CreateDbContextEvent(new EventId(459, "DisabledDebug"), LogLevel.Information, ctx);

        EFEventDataLogger.Write(
            logger,
            ev,
            writeInfoLogsAs: EFEventDataLogger.WriteInfoLogsAs.Debug,
            onlyLogExecutedCommands: false
        );

        Assert.Empty((logger).Entries);
    }

    [Fact]
    public void Write_DefaultCase_Logs_TypeName_WithEventId()
    {
        var logger = new TestLogger();
        var evId = new EventId(789, "DefaultEvent");
        var ev = CreateBaseEvent(evId, LogLevel.Debug);

        EFEventDataLogger.Write(logger, ev, onlyLogExecutedCommands: false);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal(evId, entry.EventId);
        Assert.Contains("Entity Framework", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(entry.Args);
        Assert.Equal(ev.GetType().Name, entry.Args[0]);
    }
}
