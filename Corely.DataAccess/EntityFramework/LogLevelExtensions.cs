using Microsoft.Extensions.Logging;
using static Corely.DataAccess.EntityFramework.EFEventDataLogger;

namespace Corely.DataAccess.EntityFramework;

internal static class LogLevelExtensions
{
    extension(LogLevel level)
    {
        public LogLevel WithInformationWrittenAs(WriteInfoLogsAs? infoOverride)
        {
            if (level == LogLevel.Information && infoOverride is { } overrideValue)
            {
                return overrideValue switch
                {
                    WriteInfoLogsAs.Debug => LogLevel.Debug,
                    WriteInfoLogsAs.Trace => LogLevel.Trace,
                    _ => level,
                };
            }
            return level;
        }
    }
}
