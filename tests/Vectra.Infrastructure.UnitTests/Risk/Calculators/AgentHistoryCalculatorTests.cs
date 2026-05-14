using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Risk.Calculators;

namespace Vectra.Infrastructure.UnitTests.Risk.Calculators;

public class AgentHistoryCalculatorTests
{
    private readonly IAgentHistoryRepository _historyRepo = Substitute.For<IAgentHistoryRepository>();
    private readonly AgentHistoryCalculator _sut;

    public AgentHistoryCalculatorTests()
    {
        _sut = new AgentHistoryCalculator(_historyRepo);
    }

    [Fact]
    public async Task CalculateAsync_NullHistory_FetchesFromRepo_AndReturnsBaseRisk()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.8 };
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 10, ViolationCount = 0 };
        _historyRepo.GetRecentAsync(agentId, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(history);

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThanOrEqualTo(1.0);
        await _historyRepo.Received(1).GetRecentAsync(agentId, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateAsync_HistoryProvided_DoesNotFetchFromRepo()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.8 };
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 5, ViolationCount = 0 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThanOrEqualTo(1.0);
        await _historyRepo.DidNotReceive().GetRecentAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateAsync_NullHistoryFromRepo_Returns0Point3()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.8 };
        _historyRepo.GetRecentAsync(agentId, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((AgentHistory?)null);

        var result = await _sut.CalculateAsync(context, null, CancellationToken.None);

        result.Should().Be(0.3);
    }

    [Fact]
    public async Task CalculateAsync_HighViolationRate_IncreasesRisk()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.9 };
        // 50% violation rate adds 0.25 to risk
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 10, ViolationCount = 5 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task CalculateAsync_HighRequestFrequency_IncreasesRisk()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.9 };
        // >60 rpm adds 0.3
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 400, ViolationCount = 0 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.3);
    }

    [Fact]
    public async Task CalculateAsync_LowTrustScore_IncreasesRisk()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.1 };
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 1, ViolationCount = 0 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.4);
    }

    [Fact]
    public async Task CalculateAsync_MediumTrustScore_IncreasesRiskModerately()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.5 };
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 1, ViolationCount = 0 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.2);
    }

    [Fact]
    public async Task CalculateAsync_ResultIsClampedTo1()
    {
        var agentId = Guid.NewGuid();
        // Very low trust + many violations + high rpm → risk > 1 before clamp
        var context = new RequestContext { AgentId = agentId, TrustScore = 0.0 };
        var history = new AgentHistory { AgentId = agentId, TotalRequests = 500, ViolationCount = 500 };

        var result = await _sut.CalculateAsync(context, history, CancellationToken.None);

        result.Should().Be(1.0);
    }

    [Fact]
    public void Name_ShouldBe_AgentHistory()
    {
        _sut.Name.Should().Be("agent_history");
    }

    [Fact]
    public void Weight_ShouldBe_0Point35()
    {
        _sut.Weight.Should().Be(0.35);
    }
}
