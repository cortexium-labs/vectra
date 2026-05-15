using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Application.Abstractions.Versioning;
using Vectra.BuildingBlocks.Clock;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    // ── AddSystemClock ────────────────────────────────────────────────────

    [Fact]
    public void AddSystemClock_RegistersIClock()
    {
        var services = new ServiceCollection();
        services.AddSystemClock();

        var provider = services.BuildServiceProvider();
        var clock = provider.GetRequiredService<IClock>();

        clock.Should().NotBeNull().And.BeOfType<SystemClock>();
    }

    [Fact]
    public void AddSystemClock_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddSystemClock();
        result.Should().BeSameAs(services);
    }

    // ── AddVectraVersion ──────────────────────────────────────────────────

    [Fact]
    public void AddVectraVersion_RegistersIVersion()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVectraVersion();

        var provider = services.BuildServiceProvider();
        var version = provider.GetRequiredService<IVersion>();

        version.Should().NotBeNull().And.BeOfType<Vectra.Services.VectraVersion>();
    }

    [Fact]
    public void AddVectraVersion_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddVectraVersion();
        result.Should().BeSameAs(services);
    }

    // ── AddVectraConfiguration ────────────────────────────────────────────

    [Fact]
    public void AddVectraConfiguration_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var result = services.AddVectraConfiguration(config);
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddVectraConfiguration_RegistersExpectedOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddVectraConfiguration(config);

        // Should not throw – confirms Options infrastructure is registered
        var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Vectra.BuildingBlocks.Configuration.System.SystemConfiguration>>();
        act.Should().NotThrow();
    }

    // ── AddVectraHealthChecker ────────────────────────────────────────────

    [Fact]
    public void AddVectraHealthChecker_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddVectraHealthChecker();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddVectraHealthChecker_RegistersHealthChecks()
    {
        var services = new ServiceCollection();
        services.AddVectraHealthChecker();
        // Verify the health check service type is registered
        var descriptor = services.Any(s =>
            s.ServiceType == typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService));
        descriptor.Should().BeTrue();
    }

    // ── AddVectraApiDocumentation ─────────────────────────────────────────

    [Fact]
    public void AddVectraApiDocumentation_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddVectraApiDocumentation();
        result.Should().BeSameAs(services);
    }

    // ── AddVectraProxyForwarder ───────────────────────────────────────────

    [Fact]
    public void AddVectraProxyForwarder_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddVectraProxyForwarder();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddVectraProxyForwarder_RegistersHttpClient()
    {
        var services = new ServiceCollection();
        services.AddVectraProxyForwarder();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        factory.Should().NotBeNull();
    }

    // ── AddJsonSerialization (AddHttpJsonOptions alias) ───────────────────

    [Fact]
    public void AddHttpJsonOptions_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddHttpJsonOptions();
        result.Should().BeSameAs(services);
    }
}
