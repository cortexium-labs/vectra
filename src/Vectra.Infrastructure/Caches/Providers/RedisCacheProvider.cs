using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using Vectra.Application.Abstractions.Caches;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;

namespace Vectra.Infrastructure.Caches.Providers;

public class RedisCacheProvider : ICacheProvider
{
    private readonly RedisCacheConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisCacheProvider> _logger;
    private readonly IConnectionMultiplexer _redis;
    private TimeSpan _ttl = TimeSpan.FromHours(24);
    public RedisCacheProvider(
        RedisCacheConfiguration config, 
        IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<RedisCacheProvider>>();
        _redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
    }

    public async Task SetAsync(string key, string value)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            $"hitl:{key}", 
            JsonSerializer.Serialize(value), 
            _config.TimeToLive ?? _ttl);

        _logger.LogInformation($"Redis ({_config.Address}) SET {key}");
    }

    public async Task<object?> GetAsync(object key)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"hitl:{key}");
        _logger.LogInformation($"Redis ({_config.Address}) GET {key}");
        return value.ToString();
    }

    public async Task<TItem?> GetAsync<TItem>(object key)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"hitl:{key}");
        _logger.LogInformation($"Redis ({_config.Address}) GET {key}");
        return JsonSerializer.Deserialize<TItem>(value.ToString());
    }

    public async Task<TItem> SetAsync<TItem>(object key, TItem value)
    {
        var db = _redis.GetDatabase();
        var serializedValue = JsonSerializer.Serialize(value);
        await db.StringSetAsync(
            $"hitl:{key}", 
            serializedValue, 
            _config.TimeToLive ?? _ttl);

        _logger.LogInformation($"Redis ({_config.Address}) SET {key}");
        return value;
    }
        
    public async Task<(bool success, TItem? value)> TryGetValueAsync<TItem>(string key)
    {
        var db = _redis.GetDatabase();
        var redisValue = await db.StringGetAsync($"hitl:{key}");
        if (redisValue.HasValue)
        {
            var value = JsonSerializer.Deserialize<TItem>(redisValue.ToString());
            _logger.LogInformation($"Redis ({_config.Address}) GET {key}");
            return (true, value);
        }
        return (false, default);
    }

    public async Task RemoveAsync(object key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"hitl:{key}");
        _logger.LogInformation($"Redis ({_config.Address}) DEL {key}");
    }
}