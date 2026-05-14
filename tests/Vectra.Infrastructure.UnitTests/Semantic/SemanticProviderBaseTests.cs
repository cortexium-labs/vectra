using FluentAssertions;
using Vectra.Application.Abstractions.Executions;
using Vectra.Infrastructure.Semantic.Providers;

namespace Vectra.Infrastructure.UnitTests.Semantic;

public class SemanticProviderBaseTests
{
    // Expose the protected static methods via a test subclass
    private sealed class TestSemanticProvider : SemanticProviderBase
    {
        public SemanticAnalysisResult ExposedParseResponse(string content, string providerName)
            => ParseResponse(content, providerName);

        public string ExposedComputeHash(string input)
            => ComputeHash(input);

        public Task<SemanticAnalysisResult> AnalyzeAsync(string? body, string path, CancellationToken ct)
            => Task.FromResult(new SemanticAnalysisResult { Intent = "test", Confidence = 1.0 });
    }

    private static readonly TestSemanticProvider _sut = new();

    [Fact]
    public void ParseResponse_ValidJson_ReturnsCorrectResult()
    {
        var json = """{"intent":"data_exfiltration","confidence":0.92,"risk_tags":["pii"],"explanation":"suspicious pattern"}""";

        var result = _sut.ExposedParseResponse(json, "TestProvider");

        result.Intent.Should().Be("data_exfiltration");
        result.Confidence.Should().Be(0.92);
        result.RiskTags.Should().ContainSingle().Which.Should().Be("pii");
        result.Explanation.Should().Be("suspicious pattern");
        result.FallbackSafe.Should().BeFalse("confidence >= 0.7");
    }

    [Fact]
    public void ParseResponse_LowConfidence_SetsFallbackSafeTrue()
    {
        var json = """{"intent":"read","confidence":0.5,"risk_tags":[],"explanation":"ok"}""";

        var result = _sut.ExposedParseResponse(json, "TestProvider");

        result.FallbackSafe.Should().BeTrue("confidence < 0.7");
        result.Confidence.Should().Be(0.5);
    }

    [Fact]
    public void ParseResponse_MissingExplanation_UsesDefaultExplanation()
    {
        var json = """{"intent":"write","confidence":0.8,"risk_tags":[]}""";

        var result = _sut.ExposedParseResponse(json, "MyProvider");

        result.Explanation.Should().Contain("MyProvider");
        result.Explanation.Should().Contain("write");
    }

    [Fact]
    public void ParseResponse_MissingRiskTags_UsesEmptyArray()
    {
        var json = """{"intent":"read","confidence":0.9}""";

        var result = _sut.ExposedParseResponse(json, "TestProvider");

        result.RiskTags.Should().BeEmpty();
    }

    [Fact]
    public void ParseResponse_InvalidJson_ReturnsFallbackResult()
    {
        var result = _sut.ExposedParseResponse("not-json", "TestProvider");

        result.Intent.Should().Be("unknown");
        result.Confidence.Should().Be(0.5);
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public void ParseResponse_EmptyJson_ReturnsFallback()
    {
        var result = _sut.ExposedParseResponse("{}", "TestProvider");

        result.Intent.Should().Be("unknown");
        result.FallbackSafe.Should().BeTrue();
    }

    [Fact]
    public void ParseResponse_MultipleRiskTags_ReturnsAll()
    {
        var json = """{"intent":"harmful","confidence":0.95,"risk_tags":["pii","destructive","privilege_escalation"]}""";

        var result = _sut.ExposedParseResponse(json, "TestProvider");

        result.RiskTags.Should().HaveCount(3);
        result.RiskTags.Should().Contain("destructive");
    }

    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var h1 = _sut.ExposedComputeHash("hello world");
        var h2 = _sut.ExposedComputeHash("hello world");

        h1.Should().Be(h2);
    }

    [Fact]
    public void ComputeHash_DifferentInput_ReturnsDifferentHash()
    {
        var h1 = _sut.ExposedComputeHash("hello");
        var h2 = _sut.ExposedComputeHash("world");

        h1.Should().NotBe(h2);
    }

    [Fact]
    public void ComputeHash_EmptyString_ReturnsNonEmptyHash()
    {
        var result = _sut.ExposedComputeHash(string.Empty);

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ComputeHash_ReturnsBase64String()
    {
        var result = _sut.ExposedComputeHash("test");

        var act = () => Convert.FromBase64String(result);
        act.Should().NotThrow();
    }
}
