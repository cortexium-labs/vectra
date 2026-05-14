using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.Risk;

public class RiskScoringService : IRiskScoringService
{
    private readonly RiskScoreAggregator _aggregator;
    private readonly IAgentHistoryRepository _historyRepo;
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<RiskScoringService> _logger;

    public RiskScoringService(
        RiskScoreAggregator aggregator,
        IAgentHistoryRepository historyRepo,
        ICacheService cacheService,
        ILogger<RiskScoringService> logger)
    {
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _historyRepo = historyRepo ?? throw new ArgumentNullException(nameof(historyRepo));
        _cacheProvider = cacheService.Current ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<double> ComputeRiskScoreAsync(RequestContext context, CancellationToken cancellationToken = default)
    {
        // Build a cache key based on agent ID and request fingerprint
        var cacheKey = $"risk:{context.AgentId}:{context.Method}:{context.Path}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var (success, cachedScore) = await _cacheProvider.TryGetValueAsync<double>(cacheKey);

        if (success) return cachedScore;

        var history = await _historyRepo.GetRecentAsync(context.AgentId, TimeSpan.FromMinutes(5), cancellationToken);
        var score = await _aggregator.AggregateAsync(context, history, cancellationToken);

        // Cache for a short period (e.g., 10 seconds) to avoid over‑calculation for same agent in burst
        await _cacheProvider.SetAsync(cacheKey, score);
        _logger.LogDebug("Risk score for agent {AgentId}: {Score}", context.AgentId, score);
        return score;
    }
}