using FluentAssertions;
using Vectra.Application.Abstractions.Executions;

namespace Vectra.Application.UnitTests.Abstractions.Executions;

public class HitlModelsTests
{
    [Fact]
    public void HitlDecision_ShouldStoreAllProperties()
    {
        var decided = DateTime.UtcNow;
        var decision = new HitlDecision("req-1", HitlRequestStatus.Approved, "reviewer-42", "Looks fine", decided);

        decision.Id.Should().Be("req-1");
        decision.Status.Should().Be(HitlRequestStatus.Approved);
        decision.ReviewerId.Should().Be("reviewer-42");
        decision.Comment.Should().Be("Looks fine");
        decision.DecidedAt.Should().Be(decided);
    }

    [Fact]
    public void PendingHitlRequest_ShouldStoreAllProperties()
    {
        var agentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var expires = timestamp.AddMinutes(5);
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        var request = new PendingHitlRequest(
            "req-2", "POST", "https://api.example.com/data",
            headers, "{}", "Suspicious body", agentId, timestamp, expires);

        request.Id.Should().Be("req-2");
        request.Method.Should().Be("POST");
        request.Url.Should().Be("https://api.example.com/data");
        request.Headers.Should().BeEquivalentTo(headers);
        request.Body.Should().Be("{}");
        request.Reason.Should().Be("Suspicious body");
        request.AgentId.Should().Be(agentId);
        request.Timestamp.Should().Be(timestamp);
        request.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public void HitlReplayResult_ShouldStoreAllProperties()
    {
        var responseHeaders = new Dictionary<string, string> { ["X-Result"] = "ok" };
        var body = new MemoryStream();

        var result = new HitlReplayResult(true, 200, null, responseHeaders, body);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ErrorReason.Should().BeNull();
        result.ResponseHeaders.Should().BeEquivalentTo(responseHeaders);
        result.ResponseBody.Should().BeSameAs(body);
    }

    [Fact]
    public void HitlReplayResult_Failure_ShouldStoreError()
    {
        var result = new HitlReplayResult(false, null, "timeout", null, null);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().BeNull();
        result.ErrorReason.Should().Be("timeout");
        result.ResponseHeaders.Should().BeNull();
        result.ResponseBody.Should().BeNull();
    }

    [Fact]
    public void HitlRequestStatus_AllValues_ShouldBeDefined()
    {
        Enum.IsDefined(typeof(HitlRequestStatus), HitlRequestStatus.Pending).Should().BeTrue();
        Enum.IsDefined(typeof(HitlRequestStatus), HitlRequestStatus.Approved).Should().BeTrue();
        Enum.IsDefined(typeof(HitlRequestStatus), HitlRequestStatus.Denied).Should().BeTrue();
        Enum.IsDefined(typeof(HitlRequestStatus), HitlRequestStatus.Expired).Should().BeTrue();
        Enum.IsDefined(typeof(HitlRequestStatus), HitlRequestStatus.NotFound).Should().BeTrue();
    }
}
