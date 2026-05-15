using Vectra.Domain.Primitives;

namespace Vectra.Domain.Agents;

public class Agent: AuditableEntity<Guid>
{
    public string Name { get; private set; } = null!;
    public string? OwnerId { get; private set; }
    public AgentStatus Status { get; private set; }
    public string? PolicyName { get; set; }
    public string ClientSecretHash { get; private set; } = null!;
    public double TrustScore { get; private set; }
    public ICollection<AgentHistory> AgentHistories { get; set; } = new List<AgentHistory>();

    private Agent() { } // EF Core

    public Agent(string name, string ownerId, string clientSecretHash)
    {
        Id = Guid.NewGuid();
        Name = name;
        OwnerId = ownerId;
        Status = AgentStatus.Active;
        ClientSecretHash = clientSecretHash;
        TrustScore = 0.5;
    }

    public void UpdateTrustScore(double newScore)
    {
        TrustScore = Math.Clamp(newScore, 0, 1);
    }

    public void Revoke() => Status = AgentStatus.Revoked;
}