namespace Vectra.Infrastructure.Configuration.Observability.Logging;

public sealed record SeqLoggingConfiguration
{
    public bool Enabled { get; set; } = false;
    public string LogLevel { get; init; }
    public string? ApiKey { get; init; }
    public string? Url { get; init; }

    public static SeqLoggingConfiguration Create()
    {
        return new SeqLoggingConfiguration
        {
            LogLevel = "Information"
        };
    }
}