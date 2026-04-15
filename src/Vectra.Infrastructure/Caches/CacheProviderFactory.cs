using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Caches;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Caches.Providers;

namespace Vectra.Infrastructure.Caches;

public sealed class CacheProviderFactory : ICacheProviderFactory
{
    private readonly CacheConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public CacheProviderFactory(IOptions<SystemConfiguration> options, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _config = options.Value.Storage.Cache
            ?? throw new InvalidOperationException("Cache configuration is missing.");

        _serviceProvider = serviceProvider;
    }

    public ICacheProvider Create()
    {
        return _config.DefaultProvider.ToLowerInvariant() switch
        {
            "redis" => CreateRedis(),
            "memory" => CreateMemory(),
            _ => throw new NotSupportedException($"Cache provider '{_config.DefaultProvider}' is not supported.")
        };
    }

    private ICacheProvider CreateRedis() =>
        ActivatorUtilities.CreateInstance<RedisCacheProvider>(_serviceProvider, _config.Providers.Redis);

    private ICacheProvider CreateMemory() =>
        ActivatorUtilities.CreateInstance<MemoryCacheProvider>(_serviceProvider, _config.Providers.Memory);
}