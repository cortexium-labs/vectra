namespace Vectra.Domain.Policies;

public class PolicyRule
{
    public string Name { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int Priority { get; set; } = 0;          // higher = more important
    public PolicyType Effect { get; set; } = PolicyType.Allow;
    public List<PolicyRuleCondition> Conditions { get; set; } = new();
}