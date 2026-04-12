namespace Vectra.Domain.Policies;

public class PolicyDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTime? CreatedOn { get; set; }
    public PolicyType Default { get; set; } = PolicyType.Deny;
    public List<PolicyRule> Rules { get; set; } = new();
}