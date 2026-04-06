using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Executions;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public class PolicyEngine : IPolicyEngine
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly IPolicyLoader _loader;
    private readonly ILogger<PolicyEngine> _logger;
    private const string CacheKey = "all_policies";

    public PolicyEngine(
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        IPolicyLoader loader,
        ILogger<PolicyEngine> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
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
        if (_memoryCache.TryGetValue(CacheKey, out Dictionary<Guid, PolicyDefinition>? policies) && policies != null)
            return policies;

        policies = await _loader.LoadAllAsync();
        _memoryCache.Set(CacheKey, policies, TimeSpan.FromMinutes(5)); // fallback TTL
        return policies;
    }
}