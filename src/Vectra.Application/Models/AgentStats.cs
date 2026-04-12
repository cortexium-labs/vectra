namespace Vectra.Application.Models;

public record AgentStats
{
    public Guid AgentId { get; init; }
    public int TotalRequests { get; init; }
    public int ViolationCount { get; init; }
    public double CurrentRequestsPerMinute { get; init; }
    public HashSet<string> SeenEndpoints { get; init; } = new();
    public HashSet<int> ActiveHours { get; init; } = new();
}