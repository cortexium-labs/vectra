using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Caches.Providers;

namespace Vectra.Infrastructure.UnitTests.Caches;

public class MemoryCacheProviderTests
{
    private static MemoryCacheProvider CreateSut(TimeSpan? ttl = null)
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var config = new MemoryCacheConfiguration
        {
            TimeToLive = ttl ?? TimeSpan.FromMinutes(5)
        };

        return new MemoryCacheProvider(config, sp);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var sut = CreateSut();
        await sut.SetAsync<string>("key1", "hello");

        var result = await sut.GetAsync<string>("key1");

        result.Should().Be("hello");
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var sut = CreateSut();

        var result = await sut.GetAsync<string>("non-existent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Object_MissingKey_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetAsync("non-existent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetValueAsync_ExistingKey_ReturnsSuccessAndValue()
    {
        var sut = CreateSut();
        await sut.SetAsync<int>("intKey", 42);

        var (success, value) = await sut.TryGetValueAsync<int>("intKey");

        success.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public async Task TryGetValueAsync_MissingKey_ReturnsFalse()
    {
        var sut = CreateSut();

        var (success, value) = await sut.TryGetValueAsync<string>("missing");

        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesIt()
    {
        var sut = CreateSut();
        await sut.SetAsync<string>("toRemove", "value");

        await sut.RemoveAsync("toRemove");

        var result = await sut.GetAsync<string>("toRemove");
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ReturnsStoredValue()
    {
        var sut = CreateSut();

        var returned = await sut.SetAsync("k", "stored-value");

        returned.Should().Be("stored-value");
    }

    [Fact]
    public async Task GetAsync_ObjectOverload_ReturnsStoredValue()
    {
        var sut = CreateSut();
        await sut.SetAsync<string>("objKey", "object-value");

        var result = await sut.GetAsync("objKey");

        result.Should().Be("object-value");
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        var sut = CreateSut();
        await sut.SetAsync("key", "first");
        await sut.SetAsync("key", "second");

        var result = await sut.GetAsync<string>("key");

        result.Should().Be("second");
    }
}
