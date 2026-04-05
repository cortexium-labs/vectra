using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Executions;

namespace Vectra.Infrastructure.Policy;

public sealed class PolicyCacheInvalidator : IPolicyCacheInvalidator
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly ILogger<PolicyCacheInvalidator> _logger;

    public PolicyCacheInvalidator(
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        ILogger<PolicyCacheInvalidator> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvalidateAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove($"policy:{policyId}");
        await _redisCache.RemoveAsync($"policy:{policyId}", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Policy {PolicyId} cache invalidated", policyId);
    }
}