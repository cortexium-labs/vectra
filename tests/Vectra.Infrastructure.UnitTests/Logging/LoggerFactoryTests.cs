using FluentAssertions;
using Serilog.Events;
using Vectra.Infrastructure.Logging;

namespace Vectra.Infrastructure.UnitTests.Logging;

public class LoggerFactoryTests
{
    [Theory]
    [InlineData("verbose", LogEventLevel.Verbose)]
    [InlineData("trace", LogEventLevel.Verbose)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("information", LogEventLevel.Information)]
    [InlineData("info", LogEventLevel.Information)]
    [InlineData("warning", LogEventLevel.Warning)]
    [InlineData("warn", LogEventLevel.Warning)]
    [InlineData("error", LogEventLevel.Error)]
    [InlineData("fatal", LogEventLevel.Fatal)]
    [InlineData("critical", LogEventLevel.Fatal)]
    [InlineData("VERBOSE", LogEventLevel.Verbose)]
    [InlineData("DEBUG", LogEventLevel.Debug)]
    [InlineData("ERROR", LogEventLevel.Error)]
    public void LogLevelMapper_KnownLevel_ReturnsCorrectEventLevel(string level, LogEventLevel expected)
    {
        var result = LoggerFactory.LogLevelMapper(level);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("nonsense")]
    public void LogLevelMapper_UnknownOrNull_ReturnsInformation(string? level)
    {
        var result = LoggerFactory.LogLevelMapper(level);

        result.Should().Be(LogEventLevel.Information);
    }

    [Theory]
    [InlineData("Infinite", Serilog.RollingInterval.Infinite)]
    [InlineData("Year", Serilog.RollingInterval.Year)]
    [InlineData("Month", Serilog.RollingInterval.Month)]
    [InlineData("Day", Serilog.RollingInterval.Day)]
    [InlineData("Hour", Serilog.RollingInterval.Hour)]
    [InlineData("Minute", Serilog.RollingInterval.Minute)]
    [InlineData("infinite", Serilog.RollingInterval.Infinite)]
    [InlineData("day", Serilog.RollingInterval.Day)]
    public void RollingIntervalFromString_KnownValue_ReturnsCorrectInterval(string value, Serilog.RollingInterval expected)
    {
        var result = LoggerFactory.RollingIntervalFromString(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RollingIntervalFromString_NullOrEmpty_ReturnsInfinite(string? value)
    {
        var result = LoggerFactory.RollingIntervalFromString(value);

        result.Should().Be(Serilog.RollingInterval.Infinite);
    }

    [Fact]
    public void RollingIntervalFromString_UnknownValue_ReturnsDay()
    {
        var result = LoggerFactory.RollingIntervalFromString("weekly");

        result.Should().Be(Serilog.RollingInterval.Day);
    }
}
