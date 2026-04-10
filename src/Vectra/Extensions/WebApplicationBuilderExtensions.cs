using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Serilog;
using Vectra.Infrastructure.Configuration.System;
using Vectra.Infrastructure.Configuration.System.Server;

namespace Vectra.Extensions;

public static class WebApplicationBuilderExtensions
{
    private const int DefaultHttpPort = 7080;
    private const int DefaultHttpsPort = 7443;

    public static WebApplicationBuilder ConfigureVectraHttpServer(this WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var serverConfig = scope.ServiceProvider.GetRequiredService<IOptions<SystemConfiguration>>();

        ConfigureKestrelEndpoints(builder, serverConfig.Value.Server);
        ConfigureKestrelSettings(builder);

        return builder;
    }

    private static void ConfigureKestrelEndpoints(
        WebApplicationBuilder builder,
        ServerConfiguration config)
    {
        builder.WebHost.ConfigureKestrel((_, options) =>
        {
            var httpPort = config.Http?.Port ?? DefaultHttpPort;
            int? httpsPort = GetHttpsPort(config, httpPort);

            options.ListenAnyIP(httpPort);

            if (!httpsPort.HasValue)
            {
                Log.Information($"Configuring HTTP endpoint only: HTTP {httpPort}");
                return;
            }

            ConfigureHttps(options, httpsPort.Value, config.Https, httpPort);
        });
    }

    private static void ConfigureKestrelSettings(WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBufferSize = null;
        });
    }

    private static int? GetHttpsPort(
        ServerConfiguration config,
        int httpPort)
    {
        if (config.Https?.Enabled != true) return null;

        var httpsPort = config.Https.Port ?? DefaultHttpsPort;
        if (httpsPort == httpPort)
            throw new InvalidOperationException($"HTTP and HTTPS endpoint ports cannot be the same: {httpPort}");

        return httpsPort;
    }

    private static void ConfigureHttps(
        KestrelServerOptions options,
        int httpsPort,
        HttpsServerConfiguration? httpsConfig,
        int httpPort)
    {   
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            ConfigureListenOptions(listenOptions, httpsConfig);
        });

        Log.Information($"Configuring HTTP and HTTPS endpoints: HTTP {httpPort}, HTTPS {httpsPort}");
    }

    private static void ConfigureListenOptions(
        ListenOptions listenOptions,
        HttpsServerConfiguration? httpsConfig)
    {
        var cert = httpsConfig?.Certificate;

        if (cert == null)
        {
            listenOptions.UseHttps();
            return;
        }

        ConfigureCertificate(listenOptions, cert);
    }

    private static void ConfigureCertificate(
        ListenOptions listenOptions,
        HttpsServerCertificateConfiguration cert)
    {
        if (cert == null)
            throw new ArgumentNullException(nameof(cert));

        if (string.IsNullOrWhiteSpace(cert.Password))
        {
            listenOptions.UseHttps(cert.Path);
            return;
        }

        listenOptions.UseHttps(cert.Path, cert.Password);
    }
}