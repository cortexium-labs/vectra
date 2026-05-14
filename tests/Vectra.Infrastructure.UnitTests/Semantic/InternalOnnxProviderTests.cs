using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Semantic.Providers.InternalBert;

namespace Vectra.Infrastructure.UnitTests.Semantic;

public class InternalOnnxProviderTests
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();

    public InternalOnnxProviderTests()
    {
        _cacheService.Current.Returns(_cacheProvider);
    }

    private static IOptions<SemanticConfiguration> DisabledOptions() =>
        Options.Create(new SemanticConfiguration { Enabled = false });

    [Fact]
    public void Constructor_DisabledSemantic_DoesNotLoadModel()
    {
        // Should not throw even with no PackagePath configured
        var act = () => new InternalOnnxProvider(
            DisabledOptions(), _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AnalyzeAsync_DisabledProvider_ReturnsFallback()
    {
        var sut = new InternalOnnxProvider(
            DisabledOptions(), _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        var result = await sut.AnalyzeAsync("request body", "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.Confidence.Should().Be(0.5);
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsync_DisabledProvider_NullBody_ReturnsFallback()
    {
        var sut = new InternalOnnxProvider(
            DisabledOptions(), _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        var result = await sut.AnalyzeAsync(null, "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsync_DisabledProvider_WhitespaceBody_ReturnsFallback()
    {
        var sut = new InternalOnnxProvider(
            DisabledOptions(), _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        var result = await sut.AnalyzeAsync("   ", "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DisabledProvider_DoesNotThrow()
    {
        var sut = new InternalOnnxProvider(
            DisabledOptions(), _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        var act = () => sut.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_EnabledButMissingPackagePath_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new SemanticConfiguration
        {
            Enabled = true,
            Providers = new SemanticProviders
            {
                Internal = new InternalOnnxConfiguration
                {
                    PackagePath = null
                }
            }
        });

        var act = () => new InternalOnnxProvider(
            options, _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        act.Should().Throw<InvalidOperationException>().WithMessage("*PackagePath*");
    }

    [Fact]
    public void Constructor_EnabledButFileNotFound_ThrowsFileNotFoundException()
    {
        var options = Options.Create(new SemanticConfiguration
        {
            Enabled = true,
            Providers = new SemanticProviders
            {
                Internal = new InternalOnnxConfiguration
                {
                    PackagePath = "nonexistent_model_package.zip",
                    MaxLength = 128
                }
            }
        });

        var act = () => new InternalOnnxProvider(
            options, _cacheService, NullLogger<InternalOnnxProvider>.Instance);

        act.Should().Throw<FileNotFoundException>();
    }
}
