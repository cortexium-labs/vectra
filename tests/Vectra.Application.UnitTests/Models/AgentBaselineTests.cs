using FluentAssertions;
using Vectra.Application.Models;

namespace Vectra.Application.UnitTests.Models;

public class AgentBaselineTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var baseline = new AgentBaseline();

        baseline.AverageRequestsPerMinute.Should().Be(0);
        baseline.AverageViolationRate.Should().Be(0);
        baseline.FrequentEndpoints.Should().BeEmpty();
        baseline.TypicalActiveHours.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_ShouldPersistValues()
    {
        var endpoints = new HashSet<string> { "/api/data", "/api/users" };
        var hours = new HashSet<int> { 9, 10, 11 };

        var baseline = new AgentBaseline
        {
            AverageRequestsPerMinute = 120.5,
            AverageViolationRate = 0.02,
            FrequentEndpoints = endpoints,
            TypicalActiveHours = hours
        };

        baseline.AverageRequestsPerMinute.Should().Be(120.5);
        baseline.AverageViolationRate.Should().Be(0.02);
        baseline.FrequentEndpoints.Should().BeEquivalentTo(endpoints);
        baseline.TypicalActiveHours.Should().BeEquivalentTo(hours);
    }

    [Fact]
    public void TwoBaselinesWithSameValues_ShouldBeEquivalent()
    {
        var a = new AgentBaseline { AverageRequestsPerMinute = 10, AverageViolationRate = 0.1 };
        var b = new AgentBaseline { AverageRequestsPerMinute = 10, AverageViolationRate = 0.1 };

        a.Should().BeEquivalentTo(b);
    }
}
