using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public static class RuleEvaluator
{
    public static bool EvaluateRule(PolicyRule rule, Dictionary<string, object> input, Dictionary<string, object>? data = null)
    {
        // Group conditions by logical operator? For simplicity, we assume AND at root.
        // For nested OR/AND, we need a tree structure. Here we implement flat AND/OR per rule.
        // Better: store conditions as a tree (parent-child). For MVP, we assume all top-level conditions are combined with AND.
        foreach (var condition in rule.Conditions.OrderBy(c => c.Order))
        {
            if (!PolicyEvaluator.EvaluateCondition(condition, input, data))
                return false;
        }
        return true;
    }
}