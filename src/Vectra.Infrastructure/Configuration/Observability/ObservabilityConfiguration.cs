using Vectra.Infrastructure.Configuration.Observability.Logging;

namespace Vectra.Infrastructure.Configuration.Observability;

public class ObservabilityConfiguration
{
    public LoggingConfiguration Logging { get; set; } = new();
}