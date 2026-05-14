using FluentAssertions;
using Microsoft.Extensions.Options;
using Serilog;
using Vectra.BuildingBlocks.Configuration.Observability;
using Vectra.BuildingBlocks.Configuration.Observability.Logging;
using Vectra.Infrastructure.Logging;

namespace Vectra.Infrastructure.UnitTests.Logging;

public class LoggerFactoryCreateLoggerTests
{
    private static LoggerFactory CreateSut(ObservabilityConfiguration? config = null)
    {
        config ??= new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = null,
                Seq = null
            }
        };
        return new LoggerFactory(Options.Create(config));
    }

    [Fact]
    public void CreateLogger_MinimalConfig_ReturnsLogger()
    {
        var sut = CreateSut();

        var result = sut.CreateLogger();

        result.Should().NotBeNull();
    }

    [Fact]
    public void CreateLogger_FileLoggingEnabled_ReturnsLogger()
    {
        var tempLog = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.log");
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = new FileLoggingConfiguration
                {
                    Enabled = true,
                    LogPath = tempLog,
                    LogLevel = "debug",
                    RollingInterval = "Day",
                    RetainedFileCountLimit = 3
                },
                Seq = null
            }
        };
        var sut = CreateSut(config);

        var result = sut.CreateLogger();

        result.Should().NotBeNull();
    }

    [Fact]
    public void CreateLogger_FileLoggingDisabled_DoesNotThrow()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = new FileLoggingConfiguration
                {
                    Enabled = false,
                    LogPath = "/some/path.log"
                },
                Seq = null
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_FileLoggingNullPath_DoesNotThrow()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = new FileLoggingConfiguration
                {
                    Enabled = true,
                    LogPath = null
                },
                Seq = null
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_SeqEnabled_ReturnsLogger()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = null,
                Seq = new SeqLoggingConfiguration
                {
                    Enabled = true,
                    Url = "http://localhost:5341",
                    ApiKey = "test-key",
                    LogLevel = "information"
                }
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_SeqDisabled_DoesNotConfigureSeq()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = null,
                Seq = new SeqLoggingConfiguration
                {
                    Enabled = false,
                    Url = "http://localhost:5341"
                }
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_SeqNullUrl_DoesNotConfigureSeq()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = null,
                Seq = new SeqLoggingConfiguration
                {
                    Enabled = true,
                    Url = null
                }
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_SeqWithoutApiKey_DoesNotThrow()
    {
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = null,
                Seq = new SeqLoggingConfiguration
                {
                    Enabled = true,
                    Url = "http://localhost:5341",
                    ApiKey = null
                }
            }
        };
        var sut = CreateSut(config);

        var act = () => sut.CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_BothFileSinksEnabled_ReturnsLogger()
    {
        var tempLog = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.log");
        var config = new ObservabilityConfiguration
        {
            Logging = new LoggingConfiguration
            {
                File = new FileLoggingConfiguration
                {
                    Enabled = true,
                    LogPath = tempLog,
                    LogLevel = "warning",
                    RollingInterval = "Infinite"
                },
                Seq = new SeqLoggingConfiguration
                {
                    Enabled = true,
                    Url = "http://localhost:5341"
                }
            }
        };
        var sut = CreateSut(config);

        var result = sut.CreateLogger();

        result.Should().NotBeNull();
    }
}
