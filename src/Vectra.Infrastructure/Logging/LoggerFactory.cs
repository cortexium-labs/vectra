using Serilog;
using Serilog.Events;
using Vectra.Infrastructure.Configuration.Logging;

namespace Vectra.Infrastructure.Logging;

public static class LoggerFactory
{
    public static ILogger CreateLogger(LoggingConfiguration configuration)
    {
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentUserName();

        // Console
        config = config.WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information);
        // File
        ConfigureFileSink(config, configuration.File);

        // Seq
        ConfigureSeqSink(config, configuration.Seq);

        return config.CreateLogger();
    }

    private static void ConfigureFileSink(Serilog.LoggerConfiguration config, FileLoggingConfiguration? fileLoggingConfig)
    {
        if (fileLoggingConfig == null 
            || !fileLoggingConfig.Enabled 
            || string.IsNullOrWhiteSpace(fileLoggingConfig.LogPath)) 
            return;

        config.WriteTo.File(
            path: fileLoggingConfig.LogPath,
            restrictedToMinimumLevel: LogLevelMapper(fileLoggingConfig.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingIntervalFromString(fileLoggingConfig.RollingInterval),
            retainedFileCountLimit: fileLoggingConfig.RetainedFileCountLimit ?? 7,
            shared: true
        );
    }

    private static void ConfigureSeqSink(Serilog.LoggerConfiguration config, SeqLoggingConfiguration? seqLoggingConfig)
    {
        if (seqLoggingConfig == null
            || !seqLoggingConfig.Enabled
            || string.IsNullOrWhiteSpace(seqLoggingConfig.Url)) return;

        config.WriteTo.Seq(
            serverUrl: seqLoggingConfig.Url!,
            apiKey: string.IsNullOrWhiteSpace(seqLoggingConfig.ApiKey) ? null : seqLoggingConfig.ApiKey,
            restrictedToMinimumLevel: LogLevelMapper(seqLoggingConfig.LogLevel)
        );
    }

    public static LogEventLevel LogLevelMapper(string? level) => level?.ToLowerInvariant() switch
    {
        "verbose" or "trace" => LogEventLevel.Verbose,
        "debug" => LogEventLevel.Debug,
        "information" or "info" => LogEventLevel.Information,
        "warning" or "warn" => LogEventLevel.Warning,
        "error" => LogEventLevel.Error,
        "fatal" or "critical" => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    public static RollingInterval RollingIntervalFromString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return RollingInterval.Infinite;

        return Enum.TryParse<RollingInterval>(value, true, out var interval)
            ? interval
            : RollingInterval.Day;
    }
}
