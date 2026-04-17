using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Caches;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;

namespace Vectra.Infrastructure.Caches.Providers;

public class MemoryCacheProvider : ICacheProvider
{
    private readonly MemoryCacheConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheProvider> _logger;

    public MemoryCacheProvider(
        MemoryCacheConfiguration config,
        IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _cache = _serviceProvider.GetRequiredService<IMemoryCache>();
        _logger = _serviceProvider.GetRequiredService<ILogger<MemoryCacheProvider>>();
    }

    public Task<object?> GetAsync(object key)
    {
        _cache.TryGetValue(key, out object? value);
        _logger.LogInformation($"InMemory GET {key}");
        return Task.FromResult(value);
    }

    public Task<TItem?> GetAsync<TItem>(object key)
    {
        _cache.TryGetValue(key, out TItem? value);
        _logger.LogInformation($"InMemory GET {key}");
        return Task.FromResult(value);
    }

    public Task<TItem> SetAsync<TItem>(object key, TItem value)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.TimeToLive
        };
        _cache.Set(key, value, options);
        _logger.LogInformation($"InMemory SET {key}");
        return Task.FromResult(value);
    }

    public Task<(bool success, TItem? value)> TryGetValueAsync<TItem>(string key)
    {
        var result = _cache.TryGetValue(key, out TItem? value);
        _logger.LogInformation($"InMemory GET {key}");
        return Task.FromResult((result, value));
    }

    public Task RemoveAsync(object key)
    {
        _cache.Remove(key);
        _logger.LogInformation($"InMemory DEL {key}");
        return Task.CompletedTask;
    }
}