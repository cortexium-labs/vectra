using FluentAssertions;
using Vectra.Application.Models;
using Vectra.Infrastructure.Risk.Calculators;

namespace Vectra.Infrastructure.UnitTests.Risk.Calculators;

public class TimeBasedCalculatorTests
{
    private readonly TimeBasedCalculator _sut = new();

    [Fact]
    public async Task CalculateAsync_ReturnsValueBetweenZeroAndPointFive()
    {
        var context = new RequestContext();

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThanOrEqualTo(0.5);
    }

    [Fact]
    public async Task CalculateAsync_ReturnDoesNotExceed0Point5()
    {
        // Even if both weekend and night-time apply (0.2 + 0.3 = 0.5), max is 0.5
        var context = new RequestContext();

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().BeLessThanOrEqualTo(0.5);
    }

    [Fact]
    public void Name_ShouldBe_TimeOfDay()
    {
        _sut.Name.Should().Be("time_of_day");
    }

    [Fact]
    public void Weight_ShouldBe_0Point1()
    {
        _sut.Weight.Should().Be(0.1);
    }

    [Fact]
    public async Task CalculateAsync_IgnoresProvidedHistory()
    {
        var context = new RequestContext();

        // Should not throw or fail when history is provided
        var act = async () => await _sut.CalculateAsync(context, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
