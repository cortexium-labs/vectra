namespace Vectra.Application.Models;

public record AgentBaseline
{
    public double AverageRequestsPerMinute { get; init; }
    public double AverageViolationRate { get; init; }
    public HashSet<string> FrequentEndpoints { get; init; } = new();
    public HashSet<int> TypicalActiveHours { get; init; } = new();
}