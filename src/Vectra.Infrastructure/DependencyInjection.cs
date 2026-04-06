using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Security;
using Vectra.Infrastructure.Configuration.Logging;
using Vectra.Infrastructure.Configuration.Security;
using Vectra.Infrastructure.Decision;
using Vectra.Infrastructure.Dispatchers;
using Vectra.Infrastructure.Hitl;
using Vectra.Infrastructure.Policy;
using Vectra.Infrastructure.Risk;
using Vectra.Infrastructure.Security;
using Vectra.Infrastructure.Semantic;

namespace Vectra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var agentAuthConfiguration = configuration.GetSection("AgentAuth").Get<AgentAuthConfiguration>()
                                     ?? new AgentAuthConfiguration();

        services.AddSingleton(Options.Create(agentAuthConfiguration));
        services.AddScoped<JwtTokenService>();
        services.AddScoped<ITokenService>(sp => sp.GetRequiredService<JwtTokenService>());

        // Register the selected authenticator scheme
        switch (agentAuthConfiguration.Scheme)
        {
            case AgentAuthScheme.None:
                services.AddSingleton<IAgentAuthenticator, NoneAgentAuthenticator>();
                break;

            case AgentAuthScheme.Jwt:
            default:
                services.AddScoped<IAgentAuthenticator, JwtAgentAuthenticator>();
                break;
        }

        // Policy engine
        services.AddScoped<IPolicyEngine, PolicyEngine>();
        services.AddScoped<IPolicyCacheInvalidator, PolicyCacheInvalidator>();

        // Risk scoring
        services.AddScoped<IRiskScoringService, RiskScoringService>();

        // Semantic engine (stub)
        services.AddScoped<ISemanticEngine, SemanticEngineStub>();

        services.AddMemoryCache();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // HITL (Redis)
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redis = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redis))
                throw new InvalidOperationException("Missing Redis connection string: ConnectionStrings:Redis");

            return ConnectionMultiplexer.Connect(redis);
        });

        services.AddScoped<IHitlService, RedisHitlService>();

        // Decision engine
        services.AddScoped<IDecisionEngine, DecisionEngine>();

        services.AddScoped<IDispatcher, Dispatcher>();

        // YARP forwarder
        services.AddHttpForwarder();

        return services;
    }

    public static IServiceCollection AddVectraLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var loggingConfiguration = configuration.GetSection("Logger").Get<LoggingConfiguration>()
                             ?? new LoggingConfiguration();

        Log.Logger = Logging.LoggerFactory.CreateLogger(loggingConfiguration);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        return services;
    }
}