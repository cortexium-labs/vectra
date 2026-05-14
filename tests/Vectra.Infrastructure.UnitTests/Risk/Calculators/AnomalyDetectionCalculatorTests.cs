using FluentAssertions;
using NSubstitute;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Risk.Calculators;
using Vectra.Infrastructure.Semantic;

namespace Vectra.Infrastructure.UnitTests.Risk.Calculators;

public class AnomalyDetectionCalculatorTests
{
    private readonly IAnomalyDetector _anomalyDetector = Substitute.For<IAnomalyDetector>();
    private readonly AnomalyDetectionCalculator _sut;

    public AnomalyDetectionCalculatorTests()
    {
        _sut = new AnomalyDetectionCalculator(_anomalyDetector);
    }

    [Fact]
    public async Task CalculateAsync_DelegatesToAnomalyDetector()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _anomalyDetector.DetectAsync(context, Arg.Any<CancellationToken>()).Returns(0.75);

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.75);
        await _anomalyDetector.Received(1).DetectAsync(context, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateAsync_ZeroAnomalyScore_ReturnsZero()
    {
        var context = new RequestContext();
        _anomalyDetector.DetectAsync(context, Arg.Any<CancellationToken>()).Returns(0.0);

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task CalculateAsync_PassesCancellationToken()
    {
        var context = new RequestContext();
        var cts = new CancellationTokenSource();
        _anomalyDetector.DetectAsync(context, cts.Token).Returns(0.5);

        await _sut.CalculateAsync(context, null, cts.Token);

        await _anomalyDetector.Received(1).DetectAsync(context, cts.Token);
    }

    [Fact]
    public void Name_ShouldBe_Anomaly()
    {
        _sut.Name.Should().Be("anomaly");
    }

    [Fact]
    public void Weight_ShouldBe_0Point2()
    {
        _sut.Weight.Should().Be(0.2);
    }

    [Fact]
    public async Task CalculateAsync_IgnoresProvidedHistory()
    {
        var context = new RequestContext();
        var history = new AgentHistory { TotalRequests = 100 };
        _anomalyDetector.DetectAsync(context, Arg.Any<CancellationToken>()).Returns(0.3);

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().Be(0.3);
    }
}
