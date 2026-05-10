using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.CircuitBreaker;
using Vectra.BuildingBlocks.Configuration.System.RateLimit;
using Vectra.BuildingBlocks.Configuration.System.Server;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;
using Vectra.BuildingBlocks.Configuration.System.Storage.Database;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.System;

public class SystemConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeAllSections()
    {
        var config = new SystemConfiguration();

        config.Server.Should().NotBeNull();
        config.Storage.Should().NotBeNull();
        config.RateLimit.Should().NotBeNull();
        config.CircuitBreaker.Should().NotBeNull();
    }

    [Fact]
    public void StorageConfiguration_ShouldInitializeSubSections()
    {
        var config = new StorageConfiguration();

        config.Database.Should().NotBeNull();
        config.Cache.Should().NotBeNull();
    }

    [Fact]
    public void ServerConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new ServerConfiguration();

        config.Http.Should().BeNull();
        config.Https.Should().BeNull();
        config.MaxConcurrentConnections.Should().Be(1000);
        config.MaxConcurrentUpgradedConnections.Should().Be(1000);
        config.KeepAliveTimeout.Should().Be(TimeSpan.FromMinutes(2));
        config.RequestHeadersTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.MaxRequestBodySizeMb.Should().Be(50);
    }

    [Fact]
    public void HttpServerConfiguration_DefaultPort_ShouldBe6263()
    {
        var config = new HttpServerConfiguration();

        config.Port.Should().Be(6263);
    }

    [Fact]
    public void HttpsServerConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new HttpsServerConfiguration();

        config.Enabled.Should().BeFalse();
        config.Port.Should().Be(6263);
        config.Certificate.Should().NotBeNull();
    }

    [Fact]
    public void HttpsServerCertificateConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new HttpsServerCertificateConfiguration();

        config.Path.Should().BeEmpty();
        config.Password.Should().BeEmpty();
    }

    [Fact]
    public void RateLimitConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new RateLimitConfiguration();

        config.Enabled.Should().BeTrue();
        config.DefaultRequestsPerMinute.Should().Be(60);
    }

    [Fact]
    public void RateLimitConfiguration_ShouldAllowCustomValues()
    {
        var config = new RateLimitConfiguration { Enabled = false, DefaultRequestsPerMinute = 120 };

        config.Enabled.Should().BeFalse();
        config.DefaultRequestsPerMinute.Should().Be(120);
    }

    [Fact]
    public void CircuitBreakerConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new CircuitBreakerConfiguration();

        config.Enabled.Should().BeTrue();
        config.FailureThreshold.Should().Be(5);
        config.OpenDurationSeconds.Should().Be(30);
        config.SamplingWindowSeconds.Should().Be(60);
    }

    [Fact]
    public void CircuitBreakerConfiguration_ShouldAllowCustomValues()
    {
        var config = new CircuitBreakerConfiguration
        {
            Enabled = false,
            FailureThreshold = 10,
            OpenDurationSeconds = 60,
            SamplingWindowSeconds = 120
        };

        config.Enabled.Should().BeFalse();
        config.FailureThreshold.Should().Be(10);
        config.OpenDurationSeconds.Should().Be(60);
        config.SamplingWindowSeconds.Should().Be(120);
    }

    [Fact]
    public void DatabaseConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new DatabaseConfiguration();

        config.DefaultProvider.Should().Be("Sqlite");
        config.Providers.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseProviders_ShouldInitializeAll()
    {
        var providers = new DatabaseProviders();

        providers.Sqlite.Should().NotBeNull();
        providers.Postgres.Should().NotBeNull();
    }

    [Fact]
    public void SqliteConfiguration_DefaultConnectionString_ShouldBeCorrect()
    {
        var config = new SqliteConfiguration();

        config.ConnectionString.Should().Be("Data Source=vectra.db");
    }

    [Fact]
    public void PostgreSqlConfiguration_DefaultConnectionString_ShouldBeEmpty()
    {
        var config = new PostgreSqlConfiguration();

        config.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void CacheConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new CacheConfiguration();

        config.DefaultProvider.Should().Be("Redis");
        config.Providers.Should().NotBeNull();
    }

    [Fact]
    public void CatchProviders_ShouldInitializeAll()
    {
        var providers = new CatchProviders();

        providers.Redis.Should().NotBeNull();
        providers.Memory.Should().NotBeNull();
    }

    [Fact]
    public void RedisCacheConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new RedisCacheConfiguration();

        config.TimeToLive.Should().Be(TimeSpan.FromHours(24));
        config.AbortOnConnectFail.Should().BeFalse();
        config.ConnectRetry.Should().Be(5);
        config.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MemoryCacheConfiguration_DefaultTimeToLive_ShouldBe24Hours()
    {
        var config = new MemoryCacheConfiguration();

        config.TimeToLive.Should().Be(TimeSpan.FromHours(24));
    }
}
