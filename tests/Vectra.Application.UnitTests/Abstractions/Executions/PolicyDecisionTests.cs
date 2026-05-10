using FluentAssertions;
using Vectra.Application.Abstractions.Executions;

namespace Vectra.Application.UnitTests.Abstractions.Executions;

public class PolicyDecisionTests
{
    [Fact]
    public void Allow_ShouldSetEffectToAllow()
    {
        var decision = PolicyDecision.Allow("allowed because compliant");

        decision.IsAllowed.Should().BeTrue();
        decision.IsDenied.Should().BeFalse();
        decision.IsHitl.Should().BeFalse();
        decision.Reason.Should().Be("allowed because compliant");
    }

    [Fact]
    public void Allow_WithoutReason_ShouldHaveNullReason()
    {
        var decision = PolicyDecision.Allow();
        decision.IsAllowed.Should().BeTrue();
        decision.Reason.Should().BeNull();
    }

    [Fact]
    public void Deny_ShouldSetEffectToDeny()
    {
        var decision = PolicyDecision.Deny("policy violation");

        decision.IsDenied.Should().BeTrue();
        decision.IsAllowed.Should().BeFalse();
        decision.IsHitl.Should().BeFalse();
        decision.Reason.Should().Be("policy violation");
    }

    [Fact]
    public void Deny_WithoutReason_ShouldHaveNullReason()
    {
        var decision = PolicyDecision.Deny();
        decision.IsDenied.Should().BeTrue();
        decision.Reason.Should().BeNull();
    }

    [Fact]
    public void Hitl_ShouldSetEffectToHitl()
    {
        var decision = PolicyDecision.Hitl("requires human review");

        decision.IsHitl.Should().BeTrue();
        decision.IsAllowed.Should().BeFalse();
        decision.IsDenied.Should().BeFalse();
        decision.Reason.Should().Be("requires human review");
    }

    [Fact]
    public void Hitl_WithoutReason_ShouldHaveNullReason()
    {
        var decision = PolicyDecision.Hitl();
        decision.IsHitl.Should().BeTrue();
        decision.Reason.Should().BeNull();
    }
}
