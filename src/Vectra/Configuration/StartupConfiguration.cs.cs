using Microsoft.AspNetCore.DataProtection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Vectra.Extensions;
using Vectra.Infrastructure;
using Vectra.Middleware;
using Vectra.Application;

namespace Vectra.Configuration;

[ExcludeFromCodeCoverage]
internal static class StartupConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        // Data Protection
        services.AddDataProtection()
                .SetApplicationName("VectraGateway");

        // JSON options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Vectra-specific service registrations
        services
            .AddVectraConfiguration(configuration)
            .AddSystemClock()
            .AddJsonSerialization()
            .AddCache()
            .AddInfrastructure()
            .AddVectraPersistence()
            .AddVectraApiDocumentation()
            .AddVectraProxyForwarder()
            .AddVectraHealthChecker()
            .AddVectraVersion()
            .AddVectraApplication()
            .AddVectraLogging();

        // HTTP server configuration (Kestrel, etc.)
        builder.ConfigureVectraHttpServer();
    }

    public static async Task ConfigurePipelineAsync(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseVectraCustomException();
        }

        app.UseVectraHttps();
        app.UseVectraCustomHeaders();

        app.UseRouting();
        app.UseMiddleware<AgentAuthMiddleware>();

        // Proxy branch
        app.MapWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/proxy"),
            proxyBranch => proxyBranch.UseMiddleware<ProxyMiddleware>());

        app.MapEndpoints();

        // Catch-all 404 handler
        app.Map("/{**catch-all}", async context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"No endpoint found for {context.Request.Path}");
        });

        app.UseVectraApiDocumentation();
        app.UseVectraHealthCheck();

        // Ensure database is created/migrated
        await app.EnsureApplicationDatabaseCreated();
    }
}