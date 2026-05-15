using Vectra.Domain.Policies;

namespace Vectra.Application.Abstractions.Executions;

public interface IPolicyProvider
{
    Task<PolicyDecision> EvaluateAsync(
        string? policyName, 
        Dictionary<string, object> input, 
        CancellationToken cancellationToken);
}

public record PolicyDecision
{
    public PolicyType Effect { get; init; } = PolicyType.Deny;
    public string? Reason { get; init; }

    private PolicyDecision() { }

    public static PolicyDecision Allow(string? reason = null) =>
        new() { Effect = PolicyType.Allow, Reason = reason };

    public static PolicyDecision Deny(string? reason = null) =>
        new() { Effect = PolicyType.Deny, Reason = reason };

    public static PolicyDecision Hitl(string? reason = null) =>
        new() { Effect = PolicyType.Hitl, Reason = reason };

    public bool IsAllowed => Effect == PolicyType.Allow;
    public bool IsDenied => Effect == PolicyType.Deny;
    public bool IsHitl => Effect == PolicyType.Hitl;
}