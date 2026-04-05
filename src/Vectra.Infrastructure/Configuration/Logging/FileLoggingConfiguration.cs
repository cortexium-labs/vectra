namespace Vectra.Infrastructure.Configuration.Logging;

public sealed record FileLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string LogLevel { get; init; }
    public string? LogPath { get; init; }
    public string? RollingInterval { get; init; }
    public int? RetainedFileCountLimit { get; init; }

    public static FileLoggingConfiguration Create()
    {
        return new FileLoggingConfiguration
        {
            LogLevel = "Information",
            LogPath = "logs/log-.txt",
            RollingInterval = "Day",
            RetainedFileCountLimit = 7
        };
    }
}