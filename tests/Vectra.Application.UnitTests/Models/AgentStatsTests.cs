using FluentAssertions;
using Vectra.Application.Models;

namespace Vectra.Application.UnitTests.Models;

public class AgentStatsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var stats = new AgentStats();

        stats.AgentId.Should().Be(Guid.Empty);
        stats.TotalRequests.Should().Be(0);
        stats.ViolationCount.Should().Be(0);
        stats.CurrentRequestsPerMinute.Should().Be(0);
        stats.SeenEndpoints.Should().BeEmpty();
        stats.ActiveHours.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_ShouldPersistValues()
    {
        var agentId = Guid.NewGuid();
        var endpoints = new HashSet<string> { "/api/v1/resource" };
        var hours = new HashSet<int> { 8, 14 };

        var stats = new AgentStats
        {
            AgentId = agentId,
            TotalRequests = 500,
            ViolationCount = 3,
            CurrentRequestsPerMinute = 45.5,
            SeenEndpoints = endpoints,
            ActiveHours = hours
        };

        stats.AgentId.Should().Be(agentId);
        stats.TotalRequests.Should().Be(500);
        stats.ViolationCount.Should().Be(3);
        stats.CurrentRequestsPerMinute.Should().Be(45.5);
        stats.SeenEndpoints.Should().BeEquivalentTo(endpoints);
        stats.ActiveHours.Should().BeEquivalentTo(hours);
    }

    [Fact]
    public void TwoStatsWithSameValues_ShouldBeEquivalent()
    {
        var id = Guid.NewGuid();
        var a = new AgentStats { AgentId = id, TotalRequests = 10 };
        var b = new AgentStats { AgentId = id, TotalRequests = 10 };

        a.Should().BeEquivalentTo(b);
    }
}
