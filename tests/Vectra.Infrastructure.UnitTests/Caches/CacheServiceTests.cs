using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.UnitTests.Caches;

public class CacheServiceTests
{
    [Fact]
    public void Constructor_CreatesCurrentProviderFromFactory()
    {
        var provider = Substitute.For<ICacheProvider>();
        var factory = Substitute.For<ICacheProviderFactory>();
        factory.Create().Returns(provider);

        var sut = new CacheService(factory);

        sut.Current.Should().Be(provider);
        factory.Received(1).Create();
    }

    [Fact]
    public void Constructor_FactoryReturnsNull_CurrentIsNull()
    {
        var factory = Substitute.For<ICacheProviderFactory>();
        factory.Create().Returns((ICacheProvider?)null!);

        var sut = new CacheService(factory);

        sut.Current.Should().BeNull();
    }
}
