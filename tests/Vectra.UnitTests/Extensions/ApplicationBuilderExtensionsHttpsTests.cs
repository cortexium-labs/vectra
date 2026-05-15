using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.Server;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class ApplicationBuilderExtensionsHttpsTests
{
    [Fact]
    public void UseVectraHttps_HttpsDisabled_DoesNotRedirect()
    {
        var app = BuildAppWithHttpsEnabled(false);
        var act = () => app.UseVectraHttps();
        act.Should().NotThrow();
    }

    [Fact]
    public void UseVectraHttps_ReturnsSameBuilder()
    {
        var app = BuildAppWithHttpsEnabled(false);
        var result = app.UseVectraHttps();
        result.Should().BeSameAs(app);
    }

    private static WebApplication BuildAppWithHttpsEnabled(bool httpsEnabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();

        var systemConfig = new SystemConfiguration
        {
            Server = new ServerConfiguration
            {
                Https = new HttpsServerConfiguration { Enabled = httpsEnabled }
            }
        };
        builder.Services.Configure<SystemConfiguration>(opt =>
        {
            opt.Server = systemConfig.Server;
        });

        return builder.Build();
    }
}
