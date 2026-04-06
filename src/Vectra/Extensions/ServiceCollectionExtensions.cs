using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using Vectra.Application.Abstractions.Versioning;
using Vectra.Configuration.Database;
using Vectra.Configuration.Server;
using Vectra.Exceptions;
using Vectra.Infrastructure.Persistence.Common;
using Vectra.Infrastructure.Persistence.Sqlite;
using Vectra.Services;

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
            var configuration = provider.GetRequiredService<IConfiguration>();
            var serverConfiguration = configuration.GetSection("Server").Get<ServerConfiguration>()
                     ?? new ServerConfiguration();

            return serverConfiguration;
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

    public static IServiceCollection AddVectraPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConfig = LoadDatabaseConfiguration(configuration);
        var activeConnection = dbConfig.GetActiveConnection();

        services.AddSingleton(dbConfig);
        services.AddSingleton(activeConnection);

        RegisterPersistenceLayer(services, activeConnection);

        return services;
    }

    private static void RegisterPersistenceLayer(IServiceCollection services, DatabaseConnection activeConnection)
    {
        switch (activeConnection.Provider.ToLowerInvariant())
        {
            //case "postgres":
            //    services.AddPostgresPersistenceLayer(activeConnection);
            //    break;
            case "sqlite":
                services.AddSqlitePersistenceLayer(activeConnection);
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{activeConnection.Provider}'.");
        }
    }

    private static DatabaseConfiguration LoadDatabaseConfiguration(IConfiguration configuration)
    {
        var databasesSection = configuration.GetSection(DatabaseSectionName);
        if (!databasesSection.Exists() || !databasesSection.GetChildren().Any())
            return CreateDefaultDatabaseConfiguration();

        var defaultProvider = databasesSection.GetValue<string>("Default") ?? DefaultSqliteProvider;
        var connections = new Dictionary<string, DatabaseConnection>();

        var connectionsSection = databasesSection.GetSection("Connections");
        if (!connectionsSection.Exists() || !connectionsSection.GetChildren().Any())
        {
            connections[defaultProvider] = CreateDefaultSqliteConnection();
            return new DatabaseConfiguration { Default = defaultProvider, Connections = connections };
        }

        foreach (var connSection in connectionsSection.GetChildren())
        {
            var provider = connSection.GetValue<string>("Provider") ?? DefaultSqliteProvider;
            var connectionString = connSection.GetValue<string>("ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException($"Missing ConnectionString for provider '{provider}'.");

            // Use the section key as the logical name (e.g., "Postgres", "Sqlite")
            var logicalName = connSection.Key;
            connections[logicalName] = new DatabaseConnection(provider, connectionString);
        }

        // Ensure the default provider exists in the dictionary
        if (!connections.ContainsKey(defaultProvider))
            connections[defaultProvider] = CreateDefaultSqliteConnection();

        return new DatabaseConfiguration { Default = defaultProvider, Connections = connections };
    }

    private static DatabaseConfiguration CreateDefaultDatabaseConfiguration() => new()
    {
        Default = DefaultSqliteProvider,
        Connections = new Dictionary<string, DatabaseConnection>
        {
            [DefaultSqliteProvider] = CreateDefaultSqliteConnection()
        }
    };

    private static DatabaseConnection CreateDefaultSqliteConnection() =>
            new DatabaseConnection(DefaultSqliteProvider, "Data Source=vectra.db");

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