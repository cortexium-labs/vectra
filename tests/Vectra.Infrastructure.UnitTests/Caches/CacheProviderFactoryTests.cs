using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.UnitTests.Caches;

public class CacheProviderFactoryTests
{
    private static CacheProviderFactory CreateSut(string provider, IServiceProvider? sp = null)
    {
        var config = new SystemConfiguration();
        config.Storage.Cache.DefaultProvider = provider;
        config.Storage.Cache.Providers.Memory = new MemoryCacheConfiguration { TimeToLive = TimeSpan.FromMinutes(5) };

        if (sp == null)
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddLogging();
            sp = services.BuildServiceProvider();
        }

        return new CacheProviderFactory(Options.Create(config), sp);
    }

    [Fact]
    public void Create_MemoryProvider_ReturnsMemoryCacheProvider()
    {
        var sut = CreateSut("memory");

        var provider = sut.Create();

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Create_UnsupportedProvider_ThrowsNotSupportedException()
    {
        var sut = CreateSut("unsupported");

        var act = () => sut.Create();

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var sp = services.BuildServiceProvider();

        var act = () => new CacheProviderFactory(null!, sp);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var config = new SystemConfiguration();
        config.Storage.Cache.DefaultProvider = "memory";

        var act = () => new CacheProviderFactory(Options.Create(config), null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
