using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public class PolicyEngine : IPolicyEngine
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly IPolicyRepository _policyRepository;
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        IPolicyRepository policyRepository,
        ILogger<PolicyEngine> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PolicyDecision> EvaluateAsync(Guid policyId, Dictionary<string, object> input, Dictionary<string, object>? data = null)
    {
        var policy = await GetPolicyAsync(policyId);
        if (policy == null || !policy.IsActive)
            return PolicyDecision.Deny("Policy not found or inactive");

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

    private async Task<PolicyDefinition?> GetPolicyAsync(Guid policyId)
    {
        if (_memoryCache.TryGetValue($"policy:{policyId}", out PolicyDefinition? policy))
            return policy;

        var redisKey = $"policy:{policyId}";
        var cachedJson = await _redisCache.GetStringAsync(redisKey);
        if (cachedJson != null)
        {
            policy = JsonSerializer.Deserialize<PolicyDefinition>(cachedJson);
            if (policy != null)
            {
                SetMemoryCache(policy);
                return policy;
            }
        }

        policy = await _policyRepository.GetActiveByIdAsync(policyId);
        if (policy != null)
        {
            var serialized = JsonSerializer.Serialize(policy);
            await _redisCache.SetStringAsync(redisKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            SetMemoryCache(policy);
        }

        return policy;
    }

    private void SetMemoryCache(PolicyDefinition policy)
    {
        _memoryCache.Set($"policy:{policy.Id}", policy, TimeSpan.FromMinutes(2));
    }
}