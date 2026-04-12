namespace Vectra.Domain.Policies;

public class PolicyRuleCondition
{
    public string Field { get; set; } = string.Empty;  // e.g., "method", "path", "user.role"
    public string Operator { get; set; } = string.Empty;  // eq, ne, in, notIn, startsWith, endsWith, contains, gt, lt, regex
    public object Value { get; set; } = new();
}