using Corely.DataAccess.EntityFramework;
using Microsoft.Extensions.Logging;
using static Corely.DataAccess.EntityFramework.EFEventDataLogger;

namespace Corely.DataAccess.UnitTests.EntityFramework;

public class LogLevelExtensionsTests
{
    [Theory]
    [InlineData(WriteInfoLogsAs.Debug, LogLevel.Debug)]
    [InlineData(WriteInfoLogsAs.Trace, LogLevel.Trace)]
    public void WithInformationWrittenAs_RewritesInformation(
        WriteInfoLogsAs infoOverride,
        LogLevel expected
    )
    {
        Assert.Equal(expected, LogLevel.Information.WithInformationWrittenAs(infoOverride));
    }

    [Fact]
    public void WithInformationWrittenAs_KeepsInformation_WhenNoOverride()
    {
        Assert.Equal(LogLevel.Information, LogLevel.Information.WithInformationWrittenAs(null));
    }

    [Theory]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Debug)]
    public void WithInformationWrittenAs_LeavesOtherLevelsAlone(LogLevel level)
    {
        Assert.Equal(level, level.WithInformationWrittenAs(WriteInfoLogsAs.Trace));
    }
}
