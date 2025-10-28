using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Corely.DataAccess.EntityFramework;

public static class EFEventDataLogger
{
    public enum WriteInfoLogsAs
    {
        Debug,
        Trace,
    }

    /// <summary>
    /// Writes EF EventData to the provided logger.
    /// If writeInfoLogsAs is specified and the event's level is Information, it will log at the provided level (Debug or Trace).
    /// If onlyLogExecutedCommands is true (default), only CommandExecutedEventData is logged; other EF events are ignored.
    /// Set onlyLogExecutedCommands to false to log additional EF events handled by this logger.
    /// </summary>
    public static void Write(
        ILogger logger,
        EventData eventData,
        WriteInfoLogsAs? writeInfoLogsAs = null,
        bool onlyLogExecutedCommands = true
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(eventData);

        var effectiveLevel = GetEffectiveLevel(eventData.LogLevel, writeInfoLogsAs);

        // Avoid allocating dictionaries/scopes unless the log level is enabled
        if (!logger.IsEnabled(effectiveLevel))
            return;

        switch (eventData)
        {
            case CommandExecutedEventData ced:
                LogCommandExecuted(logger, ced, effectiveLevel);
                return;

            case not CommandExecutedEventData when onlyLogExecutedCommands:
                return;

            case DbContextEventData: // catch-all for other DbContext events
                LogBasicEvent(logger, eventData, effectiveLevel);
                return;

            default:
                logger.Log(
                    effectiveLevel,
                    eventData.EventId,
                    null,
                    "Entity Framework {EFEventDataType}",
                    eventData.GetType().Name
                );
                return;
        }
    }

    private static LogLevel GetEffectiveLevel(LogLevel original, WriteInfoLogsAs? infoOverride)
    {
        if (original == LogLevel.Information && infoOverride is { } overrideValue)
        {
            return overrideValue switch
            {
                WriteInfoLogsAs.Debug => LogLevel.Debug,
                WriteInfoLogsAs.Trace => LogLevel.Trace,
                _ => original,
            };
        }
        return original;
    }

    private static void LogCommandExecuted(
        ILogger logger,
        CommandExecutedEventData e,
        LogLevel effectiveLevel
    )
    {
        var parameters = BuildParameterDictionary(e.Command.Parameters, e.LogParameterValues);
        var contextType = e.Context?.GetType().Name ?? "UnknownContext";

        var props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EFEventDataType"] = e.GetType().Name,
            ["EFDataSource"] = e.Connection.DataSource,
            ["EFDatabase"] = e.Connection.Database,
            ["EFServerVersion"] = e.Connection.ServerVersion,
            ["EFContext"] = contextType,
            ["EFDurationMs"] = e.Duration.TotalMilliseconds,
            ["EFCommandSource"] = e.CommandSource,
            ["EFExecuteMethod"] = e.ExecuteMethod,
            ["EFIsAsync"] = e.IsAsync,
            ["EFCommandId"] = e.CommandId,
            ["EFCommandType"] = e.Command.CommandType,
            ["EFCommandText"] = e.Command.CommandText,
            ["EFCommandParameters"] = parameters,
        };

        using (logger.BeginScope(props))
        {
            logger.Log(
                effectiveLevel,
                e.EventId,
                null,
                "EF {EFContext} executed {EFExecuteMethod} {EFCommandType} via {EFCommandSource} against {EFDatabase} in {EFDurationMs} ms",
                contextType,
                e.ExecuteMethod,
                e.Command.CommandType,
                e.CommandSource,
                e.Connection.Database,
                e.Duration.TotalMilliseconds
            );
        }
    }

    // New helper for simple coverage of common EF Core diagnostics events
    private static void LogBasicEvent(ILogger logger, EventData e, LogLevel effectiveLevel)
    {
        var contextType = (e as DbContextEventData)?.Context?.GetType().Name ?? "UnknownContext";
        var exception =
            (e as CommandErrorEventData)?.Exception
            ?? (e as ConnectionErrorEventData)?.Exception
            ?? (e as TransactionErrorEventData)?.Exception;

        logger.Log(
            effectiveLevel,
            e.EventId,
            exception,
            "EF event {EFEventDataType} in {EFContext}: {EFEventData}",
            e.GetType().Name,
            contextType,
            e.ToString() ?? string.Empty
        );
    }

    private static Dictionary<string, object?> BuildParameterDictionary(
        DbParameterCollection parameters,
        bool logValues
    )
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i] is DbParameter p)
            {
                var name = string.IsNullOrWhiteSpace(p.ParameterName) ? $"p{i}" : p.ParameterName;
                var value = logValues ? p.Value : "?";
                dict[name] = value;
            }
            else
            {
                dict[$"p{i}"] = logValues ? parameters[i] : "?";
            }
        }

        return dict;
    }
}
