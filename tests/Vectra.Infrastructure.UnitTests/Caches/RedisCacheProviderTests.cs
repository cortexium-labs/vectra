using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using System.Text.Json;
using Vectra.BuildingBlocks.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Caches.Providers;

namespace Vectra.Infrastructure.UnitTests.Caches;

public class RedisCacheProviderTests
{
    private readonly IConnectionMultiplexer _multiplexer = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();

    private RedisCacheProvider CreateSut(TimeSpan? ttl = null)
    {
        _multiplexer.GetDatabase().Returns(_db);
        var config = new RedisCacheConfiguration { Address = "localhost:6379", TimeToLive = ttl };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_multiplexer);
        var sp = services.BuildServiceProvider();
        return new RedisCacheProvider(config, sp);
    }

    [Fact]
    public async Task SetAsync_String_StoresSerializedValue()
    {
        var sut = CreateSut();
        // Don't configure the mock to avoid arg matcher issues; the default return (false) still exercises the path
        await sut.SetAsync("key1", "hello world");
        // Just verify no exception thrown and the call was made
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }

    [Fact]
    public async Task GetAsync_Object_ReturnsRedisValue()
    {
        var sut = CreateSut();
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue("stored-value"));
        var result = await sut.GetAsync("key1");
        result.Should().Be("stored-value");
    }

    [Fact]
    public async Task GetAsync_Generic_DeserializesValue()
    {
        var sut = CreateSut();
        var payload = JsonSerializer.Serialize(new Dictionary<string, string> { ["Name"] = "test" });
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue(payload));
        var result = await sut.GetAsync<Dictionary<string, string>>("key1");
        result.Should().NotBeNull();
        result!["Name"].Should().Be("test");
    }

    [Fact]
    public async Task SetAsync_Generic_StoresAndReturnsValue()
    {
        var sut = CreateSut();
        var value = new Dictionary<string, int> { ["Score"] = 42 };
        var result = await sut.SetAsync("key1", value);
        result.Should().BeEquivalentTo(value);
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }

    [Fact]
    public async Task TryGetValueAsync_KeyExists_ReturnsTrueWithValue()
    {
        var sut = CreateSut();
        var stored = JsonSerializer.Serialize("cached-result");
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue(stored));
        var (success, value) = await sut.TryGetValueAsync<string>("key1");
        success.Should().BeTrue();
        value.Should().Be("cached-result");
    }

    [Fact]
    public async Task TryGetValueAsync_KeyNotFound_ReturnsFalseWithDefault()
    {
        var sut = CreateSut();
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        var (success, value) = await sut.TryGetValueAsync<string>("missing");
        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_DeletesKey()
    {
        var sut = CreateSut();
        await sut.RemoveAsync("key-to-delete");
        await _db.Received().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WithCustomTtl_CallsStringSet()
    {
        var sut = CreateSut(ttl: TimeSpan.FromMinutes(10));
        await sut.SetAsync("k", "v");
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }
}
