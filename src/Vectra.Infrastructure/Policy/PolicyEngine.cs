using Microsoft.Extensions.Logging;
using System.Data;
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

    public async Task<PolicyDecision> EvaluateAsync(string policyName, Dictionary<string, object> input)
    {
        var policy = await GetPolicyAsync(policyName);
        if (policy == null)
            return PolicyDecision.Deny($"Policy {policyName} not found");

        foreach (var rule in policy.Rules.OrderByDescending(r => r.Priority))
        {
            bool matches = true;
            foreach (var cond in rule.Conditions)
            {
                if (!PolicyEvaluator.EvaluateCondition(cond, input))
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                return rule.Effect switch
                {
                    PolicyType.Allow => PolicyDecision.Allow(rule.Reason ?? "Rule allowed"),
                    PolicyType.Hitl => PolicyDecision.Hitl(rule.Reason ?? "Rule requires HITL"),
                    _ => PolicyDecision.Deny(rule.Reason ?? "Rule denied")
                };
            }
        }
        return policy.Default switch
        {
            PolicyType.Allow => PolicyDecision.Allow(),
            PolicyType.Hitl => PolicyDecision.Hitl(),
            _ => PolicyDecision.Deny()
        };
    }

    public async Task<PolicyDefinition?> GetPolicyAsync(string policyName)
    {
        var allPolicies = await GetAllPoliciesAsync();
        return allPolicies.TryGetValue(policyName, out var policy) ? policy : null;
    }

    private async Task<Dictionary<string, PolicyDefinition>> GetAllPoliciesAsync()
    {
        var (success, policies) = await _cacheProvider.TryGetValueAsync<Dictionary<string, PolicyDefinition>>(CacheKey);
        if (success && policies != null)
            return policies;

        policies = await _loader.LoadAllAsync();
        await _cacheProvider.SetAsync(CacheKey, policies);
        return policies;
    }
}