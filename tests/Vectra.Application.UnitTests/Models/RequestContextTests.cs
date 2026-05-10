using FluentAssertions;
using Vectra.Application.Models;

namespace Vectra.Application.UnitTests.Models;

public class RequestContextTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var ctx = new RequestContext();

        ctx.Method.Should().BeEmpty();
        ctx.Path.Should().BeEmpty();
        ctx.TargetUrl.Should().BeEmpty();
        ctx.Body.Should().BeNull();
        ctx.Headers.Should().BeEmpty();
        ctx.AgentId.Should().Be(Guid.Empty);
        ctx.PolicyName.Should().BeEmpty();
        ctx.TrustScore.Should().Be(0);
    }

    [Fact]
    public void SetProperties_ShouldPersistValues()
    {
        var agentId = Guid.NewGuid();
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };

        var ctx = new RequestContext
        {
            Method = "POST",
            Path = "/api/data",
            TargetUrl = "https://service.internal/api/data",
            Body = "{\"key\":\"value\"}",
            Headers = headers,
            AgentId = agentId,
            PolicyName = "strict-policy",
            TrustScore = 0.85
        };

        ctx.Method.Should().Be("POST");
        ctx.Path.Should().Be("/api/data");
        ctx.TargetUrl.Should().Be("https://service.internal/api/data");
        ctx.Body.Should().Be("{\"key\":\"value\"}");
        ctx.Headers.Should().BeEquivalentTo(headers);
        ctx.AgentId.Should().Be(agentId);
        ctx.PolicyName.Should().Be("strict-policy");
        ctx.TrustScore.Should().Be(0.85);
    }
}
