using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using Vectra.Application.Abstractions.Versioning;
using Vectra.BuildingBlocks.Clock;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Vectra.BuildingBlocks.Configuration.Observability;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.Exceptions;
using Vectra.Infrastructure.Persistence.Sqlite;
using Vectra.Services;

namespace Vectra.Extensions;

public static class ServiceCollectionExtensions
{
    private const string SystemConfigurationName = "System";
    private const string ObservabilityConfigurationName = "Observability";
    private const string SecurityConfigurationName = "Security";
    private const string SemanticConfigurationName = "Semantic";
    private const string HumanInTheLoopConfigurationName = "HumanInTheLoop";
    private const string PolicyConfigurationName = "Policy";

    #region Simple registrations

    public static IServiceCollection AddSystemClock(this IServiceCollection services)
    {
        services.AddScoped<IClock, SystemClock>();
        return services;
    }
    public static IServiceCollection AddVectraVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, VectraVersion>();
        return services;
    }

    public static IServiceCollection AddVectraConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SystemConfiguration>(configuration.GetSection(SystemConfigurationName));
        services.Configure<ObservabilityConfiguration>(configuration.GetSection(ObservabilityConfigurationName));
        services.Configure<SecurityConfiguration>(configuration.GetSection(SecurityConfigurationName));
        services.Configure<SemanticConfiguration>(configuration.GetSection(SemanticConfigurationName));
        services.Configure<HumanInTheLoopConfiguration>(configuration.GetSection(HumanInTheLoopConfigurationName));
        services.Configure<PolicyConfiguration>(configuration.GetSection(PolicyConfigurationName));

        return services;
    }

    #endregion

    #region Health checks

    public static IServiceCollection AddVectraHealthChecker(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    #endregion

    #region OpenAPI (Swagger)

    public static IServiceCollection AddVectraApiDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("vectra", new OpenApiInfo
            {
                Version = "vectra",
                Title = "Service Invocation",
                Description = "Using the service invocation API to find out how to communicate with Vectra API.",
                License = new OpenApiLicense
                {
                    Name = "Apache License Version 2.0",
                    Url = new Uri("https://www.apache.org/licenses/")
                }
            });
        });

        return services;
    }

    #endregion

    #region JSON options

    public static IServiceCollection AddHttpJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        return services;
    }

    #endregion

    #region Arguments / Version helpers

    public static IServiceCollection ParseVectraArguments(this IServiceCollection services, string[] args)
    {
        var hasStartArgument = args.Contains("--start");
        if (!hasStartArgument)
        {
            throw new StartArgumentRequiredException();
        }

        return services;
    }

    public static bool HandleVersionFlag(this string[] args)
    {
        if (args.Any(arg => arg.Equals("--version", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("-v", StringComparison.OrdinalIgnoreCase)))
        {
            var version = VectraVersion.GetApplicationVersion();
            Console.WriteLine($"Vectra Version: {version}");
            return true;
        }

        return false;
    }

    #endregion

    #region Persistence

    public static IServiceCollection AddVectraPersistence(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var systemConfig = scope.ServiceProvider.GetRequiredService<IOptions<SystemConfiguration>>().Value;

        var provider = systemConfig.Storage.Database.DefaultProvider;

        switch (provider?.ToLowerInvariant())
        {
            case "sqlite":
                services.AddSqlitePersistenceLayer();
                break;

            //case "postgres":
            //    services.AddPostgresPersistenceLayer();
            //    break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{provider}'.");
        }

        return services;
    }

    #endregion

    #region HttpClient

    public static IServiceCollection AddVectraProxyForwarder(this IServiceCollection services)
    {
        services.AddHttpClient("ProxyForwarder")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.ConnectionClose = false;
            });
        return services;
    }

    #endregion
}