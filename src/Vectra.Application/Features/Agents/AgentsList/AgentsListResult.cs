using Vectra.Domain.Agents;

namespace Vectra.Application.Features.Agents.AgentsList;

public class AgentsListResult
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OwnerId { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public string? PolicyName { get; set; } = string.Empty;
}