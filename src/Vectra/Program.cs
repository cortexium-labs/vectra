using Microsoft.AspNetCore.DataProtection;
using Vectra.Extensions;
using Vectra.Infrastructure;
using Vectra.Middleware;
using Vectra.Application;

var builder = WebApplication.CreateBuilder(args);

try
{
    ConfigureServices(builder, args);

    var app = builder.Build();
    await ConfigureWebApplication(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    await HandleStartupExceptionAsync(builder, ex);
}

static void ConfigureServices(WebApplicationBuilder builder, string[] args)
{
    var services = builder.Services;
    var config = builder.Configuration;
    var env = builder.Environment;

    services.AddDataProtection()
            .SetApplicationName("VectraGateway");

    services
        .AddInfrastructure(config)
        .AddFlowSynxPersistence()
        .AddVectraApiDocumentation()
        .AddVectraServer()
        .AddVectraProxyForwarder()
        .AddVectraHealthChecker()
        .AddVectraVersion()
        .AddVectraApplication()
        .AddVectraLoggingServices(config);

    if (!env.IsDevelopment())
        builder.Services.ParseVectraArguments(args);

    builder.ConfigureVectraHttpServer();
}

static async Task ConfigureWebApplication(WebApplication app)
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
    app.UseMiddleware<JwtMiddleware>();

    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/proxy"), proxyBranch =>
    {
        proxyBranch.UseMiddleware<ProxyMiddleware>();
    });

    app.MapEndpoints();

    app.Map("/{**catch-all}", async ctx =>
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsync($"No endpoint found for {ctx.Request.Path}");
    });

    app.UseVectraApiDocumentation();
    app.UseVectraHealthCheck();

    await app.EnsureApplicationDatabaseCreated();
}

static async Task HandleStartupExceptionAsync(WebApplicationBuilder builder, Exception ex)
{
    try
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception during startup");
    }
    catch
    {
        await Console.Error.WriteLineAsync($"Startup error: {ex.Message}");
    }

    // Prevent console from closing immediately
    await Task.Delay(500);
}