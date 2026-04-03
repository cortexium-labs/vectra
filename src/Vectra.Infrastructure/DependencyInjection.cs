using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Vectra.Core.Interfaces;
using Vectra.Core.UseCases;
using Vectra.Infrastructure.Decision;
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
        // Security
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<ITokenService, JwtTokenService>();

        // OPA client
        services.AddHttpClient<IPolicyEngine, PolicyEngine>(client =>
        {
            client.BaseAddress = new Uri(configuration["Opa:Endpoint"] ?? "http://localhost:8181");
        });

        // Risk scoring
        services.AddScoped<IRiskScoringService, RiskScoringService>();

        // Semantic engine (stub)
        services.AddScoped<ISemanticEngine, SemanticEngineStub>();

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

        // Use cases
        services.AddScoped<RegisterAgentUseCase>();
        services.AddScoped<AuthenticateAgentUseCase>();
        services.AddScoped<EvaluateRequestUseCase>();

        // YARP forwarder
        services.AddHttpForwarder();

        return services;
    }
}