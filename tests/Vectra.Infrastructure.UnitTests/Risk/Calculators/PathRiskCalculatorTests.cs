using FluentAssertions;
using Vectra.Application.Models;
using Vectra.Infrastructure.Risk.Calculators;

namespace Vectra.Infrastructure.UnitTests.Risk.Calculators;

public class PathRiskCalculatorTests
{
    private readonly PathRiskCalculator _sut = new();

    [Theory]
    [InlineData("/admin/users", 0.8)]
    [InlineData("/api/export", 0.9)]
    [InlineData("/api/dump", 0.9)]
    [InlineData("/api/bulk", 0.9)]
    [InlineData("/api/delete/item", 0.85)]
    [InlineData("/api/remove/item", 0.85)]
    [InlineData("/api/drop/table", 0.85)]
    [InlineData("/users/all", 0.95)]
    [InlineData("/users/export", 0.95)]
    [InlineData("/api/config", 0.7)]
    [InlineData("/api/settings", 0.7)]
    [InlineData("/api/env", 0.7)]
    [InlineData("/internal/service", 0.6)]
    [InlineData("/v1/endpoint", 0.2)]
    [InlineData("/api/data", 0.1)]
    public async Task CalculateAsync_Path_ReturnsExpectedRisk(string path, double expectedRisk)
    {
        var context = new RequestContext { Path = path };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(expectedRisk);
    }

    [Fact]
    public async Task CalculateAsync_PathWithMultiplePatterns_ReturnsHighestRisk()
    {
        // /users/export matches both /export (0.9) and /users/export (0.95)
        var context = new RequestContext { Path = "/users/export" };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.95);
    }

    [Fact]
    public async Task CalculateAsync_UnknownPath_ReturnsDefaultLowRisk()
    {
        var context = new RequestContext { Path = "/api/health" };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.1);
    }

    [Fact]
    public async Task CalculateAsync_CaseInsensitive_MatchesUppercase()
    {
        var context = new RequestContext { Path = "/ADMIN/users" };

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.8);
    }

    [Fact]
    public void Name_ShouldBe_Path()
    {
        _sut.Name.Should().Be("path");
    }

    [Fact]
    public void Weight_ShouldBe_0Point25()
    {
        _sut.Weight.Should().Be(0.25);
    }
}
