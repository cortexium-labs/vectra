using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.Observability;
using Vectra.BuildingBlocks.Configuration.Observability.Logging;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.Observability;

public class ObservabilityConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeLogging()
    {
        var config = new ObservabilityConfiguration();

        config.Logging.Should().NotBeNull();
    }

    [Fact]
    public void LoggingConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new LoggingConfiguration();

        config.DefaultLogLevel.Should().Be("Information");
        config.File.Should().NotBeNull();
        config.Seq.Should().NotBeNull();
    }

    [Fact]
    public void LoggingConfiguration_Create_ShouldReturnConfiguredInstance()
    {
        var config = LoggingConfiguration.Create();

        config.DefaultLogLevel.Should().Be("Information");
        config.File.LogLevel.Should().Be("Information");
        config.File.LogPath.Should().Be("logs/log-.txt");
        config.File.RollingInterval.Should().Be("Day");
        config.File.RetainedFileCountLimit.Should().Be(7);
        config.Seq.LogLevel.Should().Be("Information");
    }

    [Fact]
    public void FileLoggingConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new FileLoggingConfiguration();

        config.Enabled.Should().BeTrue();
    }

    [Fact]
    public void FileLoggingConfiguration_Create_ShouldReturnConfiguredInstance()
    {
        var config = FileLoggingConfiguration.Create();

        config.LogLevel.Should().Be("Information");
        config.LogPath.Should().Be("logs/log-.txt");
        config.RollingInterval.Should().Be("Day");
        config.RetainedFileCountLimit.Should().Be(7);
    }

    [Fact]
    public void SeqLoggingConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new SeqLoggingConfiguration();

        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SeqLoggingConfiguration_Create_ShouldReturnConfiguredInstance()
    {
        var config = SeqLoggingConfiguration.Create();

        config.LogLevel.Should().Be("Information");
        config.ApiKey.Should().BeNull();
        config.Url.Should().BeNull();
    }
}
