using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;
using StackExchange.Redis;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Security;
using Vectra.Application.Abstractions.Serializations;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Decision;
using Vectra.Infrastructure.Dispatchers;
using Vectra.Infrastructure.Hitl;
using Vectra.Infrastructure.Policy;
using Vectra.Infrastructure.Policy.Providers;
using Vectra.Infrastructure.Risk;
using Vectra.Infrastructure.Risk.Calculators;
using Vectra.Infrastructure.Security;
using Vectra.Infrastructure.Semantic;
using Vectra.Infrastructure.Serializations.Json;

namespace Vectra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, JwtTokenService>();

        // Register the selected authenticator scheme
        services.AddScoped<IAgentAuthenticator, JwtAgentAuthenticator>();
        services.AddSingleton<ISecretHasher, BcryptSecretHasher>();

        // Policy engine
        services.AddSingleton<IPolicyLoader, FileSystemPolicyLoader>();
        services.AddHttpClient("opa-policy");
        services.AddScoped<IPolicyProvider>(CreatePolicyProvider);

        // Risk scoring
        services.AddScoped<IRiskScoringService, RiskScoringService>();

        // Semantic engine (stub)
        services.AddScoped<ISemanticProvider, SemanticEngineStub>();

        services.AddMemoryCache();

        // HITL provider selection (DI + factory method)
        services.AddDistributedMemoryCache();

        services.AddScoped<IHitlService>(CreateHitlService);

        // Decision engine
        services.AddScoped<IDecisionEngine, DecisionEngine>();
        services.AddScoped<IDispatcher, Dispatcher>();

        // YARP forwarder
        services.AddHttpForwarder();
        services.AddRiskScoring();

        return services;
    }

    private static IPolicyProvider CreatePolicyProvider(IServiceProvider sp)
    {
        var policyConfiguration = sp.GetRequiredService<IOptions<PolicyConfiguration>>().Value;
        var provider = (policyConfiguration.DefaultProvider ?? "Internal").Trim();

        return provider.ToLowerInvariant() switch
        {
            "internal" => ActivatorUtilities.CreateInstance<InternalPolicyProvider>(sp),
            "opa" => ActivatorUtilities.CreateInstance<OpaPolicyProvider>(sp),
            _ => ActivatorUtilities.CreateInstance<InternalPolicyProvider>(sp)
        };
    }

    private static IHitlService CreateHitlService(IServiceProvider sp)
    {
        var systemConfiguration = sp.GetRequiredService<IOptions<SystemConfiguration>>().Value;
        var provider = systemConfiguration.Storage?.Cache?.DefaultProvider;

        return string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase)
            ? ActivatorUtilities.CreateInstance<RedisHitlService>(sp)
            : ActivatorUtilities.CreateInstance<InMemoryHitlService>(sp);
    }

    public static IServiceCollection AddVectraLogging(this IServiceCollection services)
    {
        services.AddSingleton<Logging.ILoggerFactory, Logging.LoggerFactory>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
            {
                var logger = sp.GetRequiredService<Logging.ILoggerFactory>().CreateLogger();
                Log.Logger = logger;
                return new SerilogLoggerProvider(logger, dispose: true);
            });
        });

        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProviderFactory, CacheProviderFactory>();
        services.AddSingleton<ICacheService, CacheService>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SystemConfiguration>>().Value;
            var redisConfig = config.Storage.Cache.Providers.Redis;

            var options = ConfigurationOptions.Parse(redisConfig.Address);
            options.AbortOnConnectFail = redisConfig.AbortOnConnectFail ?? false;
            options.ConnectRetry = redisConfig.ConnectRetry ?? 5;
            options.ConnectTimeout = (int)(redisConfig.ConnectTimeout?.TotalMilliseconds ?? 5000);
            options.ReconnectRetryPolicy = new ExponentialRetry((int)(redisConfig.ConnectTimeout?.TotalMilliseconds ?? 5000));
            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }

    private static IServiceCollection AddRiskScoring(this IServiceCollection services)
    {
        // Register calculators
        services.AddScoped<IRiskCalculator, MethodRiskCalculator>();
        services.AddScoped<IRiskCalculator, PathRiskCalculator>();
        services.AddScoped<IRiskCalculator, AgentHistoryCalculator>();
        services.AddScoped<IRiskCalculator, TimeBasedCalculator>();
        services.AddScoped<IRiskCalculator, BodySizeRiskCalculator>();
        services.AddScoped<IRiskCalculator, AnomalyDetectionCalculator>();

        services.AddScoped<IAnomalyDetector, StatisticalAnomalyDetector>();

        services.AddScoped<RiskScoreAggregator>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        
        return services;
    }

    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services
            .AddSingleton<ISerializer, JsonSerializer>()
            .AddSingleton<IDeserializer, JsonDeserializer>();

        return services;
    }
}