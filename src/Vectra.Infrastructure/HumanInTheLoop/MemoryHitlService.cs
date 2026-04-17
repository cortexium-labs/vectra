using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Models;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.HumanInTheLoop;

public class MemoryHitlService : IHitlService
{
    private readonly ICacheService _cache;

    public MemoryHitlService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<string> SuspendRequestAsync(RequestContext context, string reason, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString();
        var pending = new PendingHitlRequest(
            Id: id,
            Method: context.Method,
            Url: context.Path,
            Headers: context.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body: context.Body,
            AgentId: context.AgentId,
            Timestamp: DateTime.UtcNow
        );

        await _cache.Current.SetAsync($"hitl:{id}", pending);
        return id;
    }

    public async Task<PendingHitlRequest?> GetPendingAsync(string id, CancellationToken cancellationToken = default)
    {
        var (found, value) = await _cache.Current.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}");
        return found ? value : null;
    }

    public async Task ApproveAsync(string id, CancellationToken cancellationToken = default)
    {
        await _cache.Current.SetAsync($"hitl:approved:{id}", "approved");
    }

    public async Task DenyAsync(string id, CancellationToken cancellationToken = default)
    {
        await _cache.Current.SetAsync($"hitl:denied:{id}", "denied");
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        await _cache.Current.RemoveAsync($"hitl:{id}");
    }
}