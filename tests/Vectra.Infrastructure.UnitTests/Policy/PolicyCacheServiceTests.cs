using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Policy;

namespace Vectra.Infrastructure.UnitTests.Policy;

public class PolicyCacheServiceTests
{
    private readonly IPolicyLoader _policyLoader = Substitute.For<IPolicyLoader>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly IPolicyCacheService _sut;

    public PolicyCacheServiceTests()
    {
        _cacheService.Current.Returns(_cacheProvider);
        _cacheProvider.TryGetValueAsync<Dictionary<string, PolicyDefinition>>(Arg.Any<string>())
            .Returns((false, null));
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, PolicyDefinition>());

        _sut = CreateSut();
    }

    private IPolicyCacheService CreateSut()
    {
        var type = typeof(FileSystemPolicyLoader).Assembly
            .GetType("Vectra.Infrastructure.Policy.PolicyCacheService")!;
        // Create a typed NullLogger matching ILogger<PolicyCacheService> via reflection
        var typedNullLoggerType = typeof(NullLogger<>).MakeGenericType(type);
        var logger = Activator.CreateInstance(typedNullLoggerType)!;
        return (IPolicyCacheService)Activator.CreateInstance(
            type,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
            null,
            new object[] { _policyLoader, _cacheService, logger },
            null)!;
    }

    [Fact]
    public async Task GetPagedAsync_CacheMiss_LoadsFromLoader()
    {
        var policies = BuildPolicies(5);
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(policies);

        var (items, total) = await _sut.GetPagedAsync(1, 10, TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(5);
        await _policyLoader.Received(1).LoadAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_CacheHit_DoesNotCallLoader()
    {
        var policies = BuildPolicies(3);
        _cacheProvider.TryGetValueAsync<Dictionary<string, PolicyDefinition>>(Arg.Any<string>())
            .Returns((true, policies));

        var (items, total) = await _sut.GetPagedAsync(1, 10, TestContext.Current.CancellationToken);

        total.Should().Be(3);
        await _policyLoader.DidNotReceive().LoadAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_Pagination_ReturnsCorrectPage()
    {
        var policies = BuildPolicies(10);
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(policies);

        var (items, total) = await _sut.GetPagedAsync(2, 3, TestContext.Current.CancellationToken);

        total.Should().Be(10);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_ReturnsRemainingItems()
    {
        var policies = BuildPolicies(7);
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(policies);

        var (items, total) = await _sut.GetPagedAsync(3, 3, TestContext.Current.CancellationToken);

        total.Should().Be(7);
        items.Should().HaveCount(1); // 7 - 2*3 = 1
    }

    [Fact]
    public async Task GetPagedAsync_CacheMiss_CachesLoadedPolicies()
    {
        var policies = BuildPolicies(2);
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(policies);

        await _sut.GetPagedAsync(1, 10, TestContext.Current.CancellationToken);

        await _cacheProvider.Received(1)
            .SetAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, PolicyDefinition>>());
    }

    [Fact]
    public async Task GetPagedAsync_EmptyPolicies_ReturnsEmptyList()
    {
        _policyLoader.LoadAllAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, PolicyDefinition>());

        var (items, total) = await _sut.GetPagedAsync(1, 10, TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    private static Dictionary<string, PolicyDefinition> BuildPolicies(int count)
    {
        return Enumerable.Range(1, count)
            .ToDictionary(
                i => $"policy-{i}",
                i => new PolicyDefinition { Name = $"policy-{i}", Default = PolicyType.Allow });
    }
}
