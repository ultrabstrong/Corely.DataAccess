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

    public static void Write(
        ILogger logger,
        EventData eventData,
        WriteInfoLogsAs? writeInfoLogsAs = null,
        bool onlyLogExecutedCommands = true
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(eventData);

        var effectiveLevel = eventData.LogLevel.WithInformationWrittenAs(writeInfoLogsAs);

        if (!logger.IsEnabled(effectiveLevel))
            return;

        switch (eventData)
        {
            case CommandExecutedEventData ced:
                LogCommandExecuted(logger, ced, effectiveLevel);
                return;

            case not CommandExecutedEventData when onlyLogExecutedCommands:
                return;

            case DbContextEventData:
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

    private static void LogCommandExecuted(
        ILogger logger,
        CommandExecutedEventData e,
        LogLevel effectiveLevel
    )
    {
        var parameters = e.Command.Parameters.ToLoggingDictionary(e.LogParameterValues);
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
}
