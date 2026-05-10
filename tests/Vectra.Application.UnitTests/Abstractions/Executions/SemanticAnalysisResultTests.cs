using FluentAssertions;
using Vectra.Application.Abstractions.Executions;

namespace Vectra.Application.UnitTests.Abstractions.Executions;

public class SemanticAnalysisResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new SemanticAnalysisResult();

        result.Intent.Should().BeEmpty();
        result.Confidence.Should().Be(0);
        result.RiskTags.Should().BeEmpty();
        result.FallbackSafe.Should().BeFalse();
        result.Explanation.Should().BeNull();
    }

    [Fact]
    public void SetProperties_ShouldPersistValues()
    {
        var result = new SemanticAnalysisResult
        {
            Intent = "data-exfiltration",
            Confidence = 0.95,
            RiskTags = ["pii", "sensitive"],
            FallbackSafe = true,
            Explanation = "Detected suspicious pattern"
        };

        result.Intent.Should().Be("data-exfiltration");
        result.Confidence.Should().Be(0.95);
        result.RiskTags.Should().BeEquivalentTo(["pii", "sensitive"]);
        result.FallbackSafe.Should().BeTrue();
        result.Explanation.Should().Be("Detected suspicious pattern");
    }
}
