using FluentAssertions;
using Vectra.Application.Models;
using Vectra.Infrastructure.Risk.Calculators;

namespace Vectra.Infrastructure.UnitTests.Risk.Calculators;

public class MethodRiskCalculatorTests
{
    private readonly MethodRiskCalculator _sut = new();

    [Theory]
    [InlineData("GET", 0.1)]
    [InlineData("HEAD", 0.05)]
    [InlineData("OPTIONS", 0.05)]
    [InlineData("POST", 0.4)]
    [InlineData("PUT", 0.6)]
    [InlineData("PATCH", 0.5)]
    [InlineData("DELETE", 0.9)]
    [InlineData("TRACE", 0.7)]
    [InlineData("CONNECT", 0.8)]
    public async Task CalculateAsync_KnownMethod_ReturnsExpectedRisk(string method, double expectedRisk)
    {
        var context = new RequestContext { Method = method };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(expectedRisk);
    }

    [Fact]
    public async Task CalculateAsync_UnknownMethod_ReturnsDefaultRisk()
    {
        var context = new RequestContext { Method = "CUSTOM" };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.5);
    }

    [Fact]
    public async Task CalculateAsync_CaseInsensitive_MatchesLowercase()
    {
        var context = new RequestContext { Method = "delete" };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.9);
    }

    [Fact]
    public void Name_ShouldBe_Method()
    {
        _sut.Name.Should().Be("method");
    }

    [Fact]
    public void Weight_ShouldBe_0Point2()
    {
        _sut.Weight.Should().Be(0.2);
    }
}
