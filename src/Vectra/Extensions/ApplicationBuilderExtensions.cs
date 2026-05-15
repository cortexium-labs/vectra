using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Vectra.Application.Abstractions.Serializations;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.HealthCheck;
using Vectra.Infrastructure.Persistence.Common;
using Vectra.Middleware;

namespace Vectra.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseVectraCustomException(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseVectraCustomHeaders(this IApplicationBuilder app)
    {
        // Inject IVersion via middleware instead of locating from ApplicationServices
        app.UseMiddleware<VersionHeaderMiddleware>();
        return app;
    }

    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseVectraHealthCheck(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                },
                ResponseWriter = async (context, report) =>
                {
                    // Resolve per-request instead of using ApplicationServices at startup
                    var serializer = context.RequestServices.GetRequiredService<ISerializer>();

                    context.Response.ContentType = "application/json";
                    var response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        HealthCheckDuration = report.TotalDuration
                    };
                    await context.Response.WriteAsync(serializer.Serialize(response));
                }
            });
        });

        return app;
    }

    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseVectraApiDocumentation(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        app.UseSwagger(options =>
        {
            options.RouteTemplate = $"open-api/{{documentName}}/specifications.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "open-api";
            options.SwaggerEndpoint($"vectra/specifications.json", $"Vectra API");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapSwagger();
        });

        return app;
    }

    public static async Task<IApplicationBuilder> EnsureApplicationDatabaseCreated(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var initializers = serviceScope.ServiceProvider.GetServices<IDatabaseInitializer>();

        foreach (var initializer in initializers)
        {
            await initializer.EnsureDatabaseCreatedAsync();
        }

        return app;
    }

    public static IApplicationBuilder UseVectraHttps(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var serverConfiguration = scope.ServiceProvider.GetRequiredService<IOptions<SystemConfiguration>>();
        if (serverConfiguration.Value.Server.Https?.Enabled == true)
            app.UseHttpsRedirection();
        return app;
    }
}