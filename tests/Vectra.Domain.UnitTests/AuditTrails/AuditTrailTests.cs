using FluentAssertions;
using Vectra.Domain.AuditTrails;

namespace Vectra.Domain.UnitTests.AuditTrails;

public class AuditTrailTests
{
    [Fact]
    public void AuditTrail_ShouldStoreAllProperties()
    {
        var agentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var trail = new AuditTrail
        {
            AgentId = agentId,
            Action = "POST /users",
            TargetUrl = "https://api.example.com/users",
            Status = "ALLOWED",
            RiskScore = 0.2,
            Intent = "create user",
            Reason = null,
            Timestamp = timestamp
        };

        trail.AgentId.Should().Be(agentId);
        trail.Action.Should().Be("POST /users");
        trail.TargetUrl.Should().Be("https://api.example.com/users");
        trail.Status.Should().Be("ALLOWED");
        trail.RiskScore.Should().Be(0.2);
        trail.Intent.Should().Be("create user");
        trail.Reason.Should().BeNull();
        trail.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData("ALLOWED")]
    [InlineData("DENIED")]
    [InlineData("PENDING_HITL")]
    public void AuditTrail_ShouldAcceptKnownStatusValues(string status)
    {
        var trail = new AuditTrail { Status = status };

        trail.Status.Should().Be(status);
    }

    [Fact]
    public void AuditTrail_RiskScore_ShouldBeNullable()
    {
        var trail = new AuditTrail { RiskScore = null };

        trail.RiskScore.Should().BeNull();
    }

    [Fact]
    public void AuditTrail_ShouldAssignId()
    {
        var trail = new AuditTrail { Id = 42 };

        trail.Id.Should().Be(42);
    }

    [Fact]
    public void AuditTrail_DefaultValues_ShouldBeEmptyStrings()
    {
        var trail = new AuditTrail();

        trail.Action.Should().BeEmpty();
        trail.TargetUrl.Should().BeEmpty();
        trail.Status.Should().BeEmpty();
        trail.Intent.Should().BeNull();
        trail.Reason.Should().BeNull();
        trail.Timestamp.Should().BeNull();
    }
}
