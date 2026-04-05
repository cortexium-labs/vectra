namespace Vectra.Infrastructure.Configuration.Logging;

public sealed record LoggingConfiguration
{
    public string DefaultLogLevel { get; init; } = "Information";
    public FileLoggingConfiguration File { get; init; } = new();
    public SeqLoggingConfiguration Seq { get; set; } = new();

    public static LoggingConfiguration Create()
    {
        return new LoggingConfiguration
        {
            File = FileLoggingConfiguration.Create(),
            Seq = SeqLoggingConfiguration.Create()
        };
    }
}