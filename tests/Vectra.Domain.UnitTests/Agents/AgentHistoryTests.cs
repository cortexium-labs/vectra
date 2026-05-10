using FluentAssertions;
using Vectra.Domain.Agents;

namespace Vectra.Domain.UnitTests.Agents;

public class AgentHistoryTests
{
    [Fact]
    public void AgentHistory_ShouldStoreAllProperties()
    {
        var agentId = Guid.NewGuid();
        var windowStart = DateTime.UtcNow;

        var history = new AgentHistory
        {
            AgentId = agentId,
            WindowStart = windowStart,
            WindowDurationSeconds = 120,
            TotalRequests = 50,
            ViolationCount = 3,
            AverageRiskScore = 0.4
        };

        history.AgentId.Should().Be(agentId);
        history.WindowStart.Should().Be(windowStart);
        history.WindowDurationSeconds.Should().Be(120);
        history.TotalRequests.Should().Be(50);
        history.ViolationCount.Should().Be(3);
        history.AverageRiskScore.Should().Be(0.4);
    }

    [Fact]
    public void AgentHistory_ShouldHaveDefaultWindowDurationOfSixtySeconds()
    {
        var history = new AgentHistory();

        history.WindowDurationSeconds.Should().Be(60);
    }

    [Fact]
    public void AgentHistory_ShouldLinkToAgent()
    {
        var agent = new Agent("TestAgent", "owner-1", "hash");
        var history = new AgentHistory
        {
            AgentId = agent.Id,
            Agent = agent
        };

        history.Agent.Should().Be(agent);
        history.AgentId.Should().Be(agent.Id);
    }

    [Fact]
    public void AgentHistory_ShouldAllowZeroRequestsAndViolations()
    {
        var history = new AgentHistory
        {
            TotalRequests = 0,
            ViolationCount = 0,
            AverageRiskScore = 0.0
        };

        history.TotalRequests.Should().Be(0);
        history.ViolationCount.Should().Be(0);
        history.AverageRiskScore.Should().Be(0.0);
    }
}
