using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class HitlsEndpointTests
{
    private readonly IHitlService _hitlService;

    public HitlsEndpointTests()
    {
        _hitlService = Substitute.For<IHitlService>();
    }

    // ── GetAllPending ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPending_ReturnsList()
    {
        var pending = new List<PendingHitlRequest>
        {
            new("id1", "GET", "http://test", [], null, "reason", Guid.NewGuid(),
                DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5))
        };
        _hitlService.GetAllPendingAsync(Arg.Any<CancellationToken>()).Returns(pending);

        var result = await Hitls.GetAllPending(_hitlService, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task GetAllPending_EmptyList_Returns200()
    {
        _hitlService.GetAllPendingAsync(Arg.Any<CancellationToken>()).Returns(new List<PendingHitlRequest>());

        var result = await Hitls.GetAllPending(_hitlService, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    // ── GetStatus ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_NotFound_Returns404()
    {
        _hitlService.GetStatusAsync("missing", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.NotFound);

        var result = await Hitls.GetStatus("missing", _hitlService, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task GetStatus_Approved_Returns200WithStatus()
    {
        _hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Approved);

        var result = await Hitls.GetStatus("id1", _hitlService, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task GetStatus_Pending_IncludesPendingRequest()
    {
        var pending = new PendingHitlRequest("id1", "GET", "http://x", [], null, "reason",
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        _hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Pending);
        _hitlService.GetPendingAsync("id1", Arg.Any<CancellationToken>()).Returns(pending);

        var result = await Hitls.GetStatus("id1", _hitlService, CancellationToken.None);

        AssertStatusCode(result, 200);
        await _hitlService.Received(1).GetPendingAsync("id1", Arg.Any<CancellationToken>());
    }

    // ── ApproveHitl ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveHitl_NotFound_Returns404()
    {
        _hitlService.GetStatusAsync("nope", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.NotFound);

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl("nope", body, context, _hitlService, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task ApproveHitl_Expired_Returns404()
    {
        _hitlService.GetStatusAsync("old", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Expired);

        var body = new Hitls.ReviewDecisionRequest("late");
        var context = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl("old", body, context, _hitlService, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task ApproveHitl_Success_ReturnsStreamResult()
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("ok"));
        var replay = new HitlReplayResult(true, 200, null, null, stream);

        _hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", Arg.Any<CancellationToken>()).Returns(replay);

        var body = new Hitls.ReviewDecisionRequest("LGTM");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.ApproveHitl("id1", body, context, _hitlService, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveHitl_ReplayFails503_Returns502()
    {
        var replay = new HitlReplayResult(false, 503, "upstream down", null, null);

        _hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", Arg.Any<CancellationToken>()).Returns(replay);

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.ApproveHitl("id1", body, context, _hitlService, CancellationToken.None);

        AssertStatusCode(result, 502);
    }

    // ── DenyHitl ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DenyHitl_NotFound_Returns404()
    {
        _hitlService.GetStatusAsync("nope", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.NotFound);

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        var result = await Hitls.DenyHitl("nope", body, context, _hitlService, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task DenyHitl_Approved_Returns200()
    {
        _hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Approved);
        _hitlService.DenyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);

        var body = new Hitls.ReviewDecisionRequest("denied");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.DenyHitl("id1", body, context, _hitlService, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertStatusCode(IResult httpResult, int expected)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expected);
}
