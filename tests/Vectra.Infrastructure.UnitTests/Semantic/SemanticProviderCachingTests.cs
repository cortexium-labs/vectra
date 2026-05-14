using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Semantic.Providers.AzureAi;
using Vectra.Infrastructure.Semantic.Providers.Gemini;
using Vectra.Infrastructure.Semantic.Providers.Ollama;
using Vectra.Infrastructure.Semantic.Providers.OpenAi;

namespace Vectra.Infrastructure.UnitTests.Semantic;

/// <summary>
/// Tests the cacheable early-return paths for all cloud-backed semantic providers
/// (null/empty body, and cache-hit paths). These are the testable portions that
/// don't require live API credentials.
/// </summary>
public class SemanticProviderCachingTests
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();

    public SemanticProviderCachingTests()
    {
        _cacheService.Current.Returns(_cacheProvider);
    }

    private IOptions<SemanticConfiguration> BuildOptions(string provider = "openai") =>
        Options.Create(new SemanticConfiguration
        {
            Enabled = true,
            DefaultProvider = provider,
            Providers = new SemanticProviders
            {
                OpenAi = new OpenAiConfiguration
                {
                    ApiKey = "sk-test-key",
                    Model = "gpt-4o-mini"
                },
                Gemini = new GeminiConfiguration
                {
                    ApiKey = "AIzaSyDtest_key_for_unit_testing_012345",
                    Model = "gemini-1.5-flash",
                    Temperature = 0.0,
                    MaxTokens = 100
                },
                Ollama = new OllamaConfiguration
                {
                    Endpoint = "http://localhost:11434",
                    Model = "llama3"
                },
                AzureAi = new AzureAiConfiguration
                {
                    Endpoint = "https://example.openai.azure.com",
                    ApiKey = "azure-test-key",
                    Model = "gpt-4"
                }
            }
        });

    // ── OpenAI Provider ───────────────────────────────────────────────────

    [Fact]
    public async Task OpenAiProvider_NullBody_ReturnsFallback()
    {
        var sut = new OpenAiProvider(BuildOptions(), _cacheService, NullLogger<OpenAiProvider>.Instance);

        var result = await sut.AnalyzeAsync(null, "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAiProvider_WhitespaceBody_ReturnsFallback()
    {
        var sut = new OpenAiProvider(BuildOptions(), _cacheService, NullLogger<OpenAiProvider>.Instance);

        var result = await sut.AnalyzeAsync("   ", "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAiProvider_CacheHit_ReturnsCachedResult()
    {
        var cached = new SemanticAnalysisResult { Intent = "read", Confidence = 0.9, FallbackSafe = false };
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((true, cached));

        var sut = new OpenAiProvider(BuildOptions(), _cacheService, NullLogger<OpenAiProvider>.Instance);

        var result = await sut.AnalyzeAsync("some body content", "path", CancellationToken.None);

        result.Should().Be(cached);
    }

    [Fact]
    public async Task OpenAiProvider_ApiCallFails_ReturnsFallback()
    {
        // Cache miss → API call → exception → fallback
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((false, null));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<SemanticAnalysisResult>())
            .Returns(x => Task.FromResult(x.Arg<SemanticAnalysisResult>()));

        var sut = new OpenAiProvider(BuildOptions(), _cacheService, NullLogger<OpenAiProvider>.Instance);

        // Intentionally uses invalid key — API call will throw
        var result = await sut.AnalyzeAsync("some request body", "/api/test", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    // ── Gemini Provider ───────────────────────────────────────────────────

    [Fact]
    public async Task GeminiProvider_NullBody_ReturnsFallback()
    {
        var sut = new GeminiProvider(BuildOptions(), _cacheService, NullLogger<GeminiProvider>.Instance);

        var result = await sut.AnalyzeAsync(null, "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task GeminiProvider_WhitespaceBody_ReturnsFallback()
    {
        var sut = new GeminiProvider(BuildOptions(), _cacheService, NullLogger<GeminiProvider>.Instance);

        var result = await sut.AnalyzeAsync("  ", "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task GeminiProvider_CacheHit_ReturnsCachedResult()
    {
        var cached = new SemanticAnalysisResult { Intent = "write", Confidence = 0.85, FallbackSafe = false };
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((true, cached));

        var sut = new GeminiProvider(BuildOptions(), _cacheService, NullLogger<GeminiProvider>.Instance);

        var result = await sut.AnalyzeAsync("body", "path", CancellationToken.None);

        result.Should().Be(cached);
    }

    [Fact]
    public async Task GeminiProvider_ApiCallFails_ReturnsFallback()
    {
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((false, null));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<SemanticAnalysisResult>())
            .Returns(x => Task.FromResult(x.Arg<SemanticAnalysisResult>()));

        var sut = new GeminiProvider(BuildOptions(), _cacheService, NullLogger<GeminiProvider>.Instance);

        var result = await sut.AnalyzeAsync("request body content", "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    // ── Ollama Provider ───────────────────────────────────────────────────

    [Fact]
    public async Task OllamaProvider_NullBody_ReturnsFallback()
    {
        var sut = new OllamaProvider(BuildOptions(), _cacheService, NullLogger<OllamaProvider>.Instance);

        var result = await sut.AnalyzeAsync(null, "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task OllamaProvider_WhitespaceBody_ReturnsFallback()
    {
        var sut = new OllamaProvider(BuildOptions(), _cacheService, NullLogger<OllamaProvider>.Instance);

        var result = await sut.AnalyzeAsync("", "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task OllamaProvider_CacheHit_ReturnsCachedResult()
    {
        var cached = new SemanticAnalysisResult { Intent = "harmful", Confidence = 0.92, FallbackSafe = false };
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((true, cached));

        var sut = new OllamaProvider(BuildOptions(), _cacheService, NullLogger<OllamaProvider>.Instance);

        var result = await sut.AnalyzeAsync("body", "path", CancellationToken.None);

        result.Should().Be(cached);
    }

    [Fact]
    public async Task OllamaProvider_ApiCallFails_ReturnsFallback()
    {
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((false, null));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<SemanticAnalysisResult>())
            .Returns(x => Task.FromResult(x.Arg<SemanticAnalysisResult>()));

        var sut = new OllamaProvider(BuildOptions(), _cacheService, NullLogger<OllamaProvider>.Instance);

        var result = await sut.AnalyzeAsync("request body content", "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    // ── AzureAI Provider ──────────────────────────────────────────────────

    [Fact]
    public async Task AzureAiProvider_NullBody_ReturnsFallback()
    {
        var sut = new AzureAiProvider(BuildOptions(), _cacheService, NullLogger<AzureAiProvider>.Instance);

        var result = await sut.AnalyzeAsync(null, "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task AzureAiProvider_WhitespaceBody_ReturnsFallback()
    {
        var sut = new AzureAiProvider(BuildOptions(), _cacheService, NullLogger<AzureAiProvider>.Instance);

        var result = await sut.AnalyzeAsync("  ", "path", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public async Task AzureAiProvider_CacheHit_ReturnsCachedResult()
    {
        var cached = new SemanticAnalysisResult { Intent = "admin_action", Confidence = 0.88, FallbackSafe = false };
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((true, cached));

        var sut = new AzureAiProvider(BuildOptions(), _cacheService, NullLogger<AzureAiProvider>.Instance);

        var result = await sut.AnalyzeAsync("body", "path", CancellationToken.None);

        result.Should().Be(cached);
    }

    [Fact]
    public async Task AzureAiProvider_ApiCallFails_ReturnsFallback()
    {
        _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(Arg.Any<string>())
            .Returns((false, null));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<SemanticAnalysisResult>())
            .Returns(x => Task.FromResult(x.Arg<SemanticAnalysisResult>()));

        var sut = new AzureAiProvider(BuildOptions(), _cacheService, NullLogger<AzureAiProvider>.Instance);

        var result = await sut.AnalyzeAsync("request body content", "/api", CancellationToken.None);

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }
}
