using StackExchange.Redis;
using System.Text.Json;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Models;

namespace Vectra.Infrastructure.Hitl;

public class RedisHitlService : IHitlService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    public RedisHitlService(IConnectionMultiplexer redis)
    {
        _redis = redis;
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

        var db = _redis.GetDatabase();
        await db.StringSetAsync($"hitl:{id}", JsonSerializer.Serialize(pending), _ttl);
        return id;
    }

    public async Task<PendingHitlRequest?> GetPendingAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"hitl:{id}");
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<PendingHitlRequest>(value.ToString());
    }

    public async Task ApproveAsync(string id, CancellationToken cancellationToken = default)
    {
        // This would resume the request – handled by the gateway middleware.
        // For now, we just mark it as approved (store in Redis).
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"hitl:approved:{id}", "approved", TimeSpan.FromMinutes(5));
    }

    public async Task DenyAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"hitl:denied:{id}", "denied", TimeSpan.FromMinutes(5));
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"hitl:{id}");
    }
}