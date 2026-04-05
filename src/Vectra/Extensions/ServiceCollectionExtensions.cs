using FlowSynx.Configuration.Database;
using Vectra.Infrastructure.Persistence.Sqlite;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using Vectra.BuildingBlocks.Clock;
using Vectra.Infrastructure.Persistence.Abstractions;
using Vectra.Configuration.Database;
using Vectra.Infrastructure.Persistence.Sqlite.Configuration;
using Vectra.Configuration.Server;
using Vectra.Exceptions;
using Vectra.Services;
using Vectra.Application.Abstractions.Versioning;
using Vectra.Infrastructure;

namespace Vectra.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultSqliteProvider = "SQLite";
    private const string DatabaseSectionName = "Databases";

    #region Simple registrations

    public static IServiceCollection AddVectraVersion(this IServiceCollection services)
    {
        services.AddSingleton<IVersion, VectraVersion>();
        return services;
    }

    public static IServiceCollection AddVectraServer(this IServiceCollection services)
    {
        services.AddScoped(provider =>
        {
            var tenantConfig = provider.GetRequiredService<IConfiguration>();
            return tenantConfig.BindSection<ServerConfiguration>("System:Server");
        });

        return services;
    }

    #endregion

    #region Logging

    public static void AddVectraLoggingFilter(this ILoggingBuilder builder)
    {
        builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        
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
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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

    public static IServiceCollection AddFlowSynxPersistence(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var tenantConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var dbConfig = LoadDatabaseConfiguration(tenantConfig);
        var activeConnection = dbConfig.GetActiveConnection();

        services.AddSingleton(dbConfig);
        services.AddSingleton(activeConnection);
        services.AddSingleton<IDatabaseProvider>(new DatabaseProvider(activeConnection.Provider));

        RegisterPersistenceLayer(services, activeConnection);

        return services;
    }

    private static void RegisterPersistenceLayer(IServiceCollection services, DatabaseConnection activeConnection)
    {
        switch (activeConnection.Provider.ToLowerInvariant())
        {
            case "postgres":
                //services.AddPostgresPersistenceLayer(activeConnection);
                break;

            case "sqlite":
                services.AddSqlitePersistenceLayer(activeConnection);
                break;

            default:
                throw new InvalidOperationException($"Unsupported database provider '{activeConnection.Provider}'.");
        }
    }

    private static DatabaseConfiguration LoadDatabaseConfiguration(IConfiguration tenantConfig)
    {
        var configuration = tenantConfig;
        var databasesSection = configuration.GetSection(DatabaseSectionName);

        if (!databasesSection.Exists() || !databasesSection.GetChildren().Any())
            return CreateDefaultDatabaseConfiguration();

        var config = new DatabaseConfiguration
        {
            Default = databasesSection.GetValue<string>("Default") ?? DefaultSqliteProvider
        };

        var connectionsSection = databasesSection.GetSection("Connections");
        if (!connectionsSection.Exists() || !connectionsSection.GetChildren().Any())
        {
            config.Connections[DefaultSqliteProvider] = CreateDefaultSqliteConnection();
            return config;
        }

        foreach (var connectionSection in connectionsSection.GetChildren())
        {
            var provider = connectionSection.GetValue<string>("Provider") ?? DefaultSqliteProvider;

            DatabaseConnection connection = provider.ToLowerInvariant() switch
            {
                //"postgres" => connectionSection.Get<PostgreDatabaseConnection>()!,
                "sqlite" => connectionSection.Get<SqliteDatabaseConnection>()!,
                _ => throw new InvalidOperationException($"Unsupported provider: {provider}")
            };

            connection.Provider = provider;
            config.Connections[connectionSection.Key] = connection;
        }

        return config;
    }

    private static DatabaseConfiguration CreateDefaultDatabaseConfiguration() => new()
    {
        Default = DefaultSqliteProvider,
        Connections = new Dictionary<string, DatabaseConnection>
        {
            [DefaultSqliteProvider] = CreateDefaultSqliteConnection()
        }
    };

    private static SqliteDatabaseConnection CreateDefaultSqliteConnection() => new()
    {
        Provider = DefaultSqliteProvider,
        FilePath = "flowsynx.db"
    };

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