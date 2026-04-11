using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.Policy;

public class PolicyEngine : IPolicyEngine
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IPolicyLoader _loader;
    private readonly ILogger<PolicyEngine> _logger;
    private const string CacheKey = "all_policies";

    public PolicyEngine(
        ICacheService cacheService,
        IPolicyLoader loader,
        ILogger<PolicyEngine> logger)
    {
        _cacheProvider = cacheService.Current ?? throw new ArgumentNullException(nameof(cacheService));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PolicyDecision> EvaluateAsync(Guid policyId, Dictionary<string, object> input, Dictionary<string, object>? data = null)
    {
        var policy = await GetPolicyAsync(policyId);
        if (policy == null)
            return PolicyDecision.Deny($"Policy {policyId} not found");

        var applicableRules = new List<PolicyRule>();
        foreach (var rule in policy.Rules.OrderByDescending(r => r.Priority))
        {
            if (RuleEvaluator.EvaluateRule(rule, input, data))
                applicableRules.Add(rule);
        }

        var chosen = applicableRules.FirstOrDefault();
        if (chosen == null)
            return PolicyDecision.Deny("No matching rule");

        return chosen.Effect switch
        {
            "allow" => PolicyDecision.Allow(chosen.Reason ?? "Rule allowed"),
            "hitl" => PolicyDecision.Hitl(chosen.Reason ?? "Rule requires HITL"),
            _ => PolicyDecision.Deny(chosen.Reason ?? "Rule denied")
        };
    }

    public async Task<PolicyDefinition?> GetPolicyAsync(Guid policyId)
    {
        var allPolicies = await GetAllPoliciesAsync();
        return allPolicies.TryGetValue(policyId, out var policy) ? policy : null;
    }

    private async Task<Dictionary<Guid, PolicyDefinition>> GetAllPoliciesAsync()
    {
        var (success, policies) = await _cacheProvider.TryGetValueAsync<Dictionary<Guid, PolicyDefinition>>(CacheKey);
        if (success && policies != null)
            return policies;

        policies = await _loader.LoadAllAsync();
        await _cacheProvider.SetAsync(CacheKey, policies);
        return policies;
    }
}