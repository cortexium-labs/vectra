using FluentAssertions;
using NSubstitute;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Risk;

namespace Vectra.Infrastructure.UnitTests.Risk;

public class RiskScoreAggregatorTests
{
    private static IRiskCalculator CreateCalculator(string name, double weight, double score)
    {
        var calc = Substitute.For<IRiskCalculator>();
        calc.Name.Returns(name);
        calc.Weight.Returns(weight);
        calc.CalculateAsync(Arg.Any<RequestContext>(), Arg.Any<AgentHistory?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(score));
        return calc;
    }

    [Fact]
    public async Task AggregateAsync_NoCalculators_ReturnsZero()
    {
        var sut = new RiskScoreAggregator(Array.Empty<IRiskCalculator>());

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task AggregateAsync_SingleCalculator_ReturnsItsScore()
    {
        var calc = CreateCalculator("test", 1.0, 0.7);
        var sut = new RiskScoreAggregator([calc]);

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().BeApproximately(0.7, 1e-9);
    }

    [Fact]
    public async Task AggregateAsync_MultipleCalculators_ReturnsWeightedAverage()
    {
        // (0.5 * 2 + 0.8 * 1) / (2 + 1) = (1.0 + 0.8) / 3 ≈ 0.6
        var calc1 = CreateCalculator("c1", 2.0, 0.5);
        var calc2 = CreateCalculator("c2", 1.0, 0.8);
        var sut = new RiskScoreAggregator([calc1, calc2]);

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().BeApproximately(0.6, 1e-9);
    }

    [Fact]
    public async Task AggregateAsync_ResultClampedToZeroWhenAllScoresZero()
    {
        var calc = CreateCalculator("c1", 1.0, 0.0);
        var sut = new RiskScoreAggregator([calc]);

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task AggregateAsync_ResultClampedToOne()
    {
        var calc = CreateCalculator("c1", 1.0, 1.5); // score > 1
        var sut = new RiskScoreAggregator([calc]);

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().Be(1.0);
    }

    [Fact]
    public async Task AggregateAsync_PassesContextAndHistoryToCalculators()
    {
        var context = new RequestContext { Method = "DELETE" };
        var history = new AgentHistory();
        var calc = CreateCalculator("c1", 1.0, 0.5);
        var sut = new RiskScoreAggregator([calc]);

        await sut.AggregateAsync(context, history, CancellationToken.None);

        await calc.Received(1).CalculateAsync(context, history, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AggregateAsync_AllCalculatorsRunConcurrently()
    {
        var calcs = Enumerable.Range(1, 5)
            .Select(i => CreateCalculator($"c{i}", 1.0, 0.5))
            .ToList();
        var sut = new RiskScoreAggregator(calcs);

        var result = await sut.AggregateAsync(new RequestContext(), null, CancellationToken.None);

        result.Should().BeApproximately(0.5, 1e-9);
        foreach (var calc in calcs)
            await calc.Received(1).CalculateAsync(Arg.Any<RequestContext>(), Arg.Any<AgentHistory?>(), Arg.Any<CancellationToken>());
    }
}
