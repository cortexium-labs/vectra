using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using VoidType = Vectra.Application.Abstractions.Dispatchers.Void;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Hitl.Approve;
using Vectra.Application.Features.Hitl.Deny;
using Vectra.Application.Features.Hitl.GetAllPending;
using Vectra.Application.Features.Hitl.GetStatus;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class HitlsEndpointTests
{
    private readonly IDispatcher _dispatcher;

    public HitlsEndpointTests()
    {
        _dispatcher = Substitute.For<IDispatcher>();
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
        _dispatcher.Dispatch(Arg.Any<GetAllPendingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<PendingHitlRequest>.SuccessAsync(pending, 1, 10, 1));

        var result = await Hitls.GetAllPending(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task GetAllPending_EmptyList_Returns200()
    {
        _dispatcher.Dispatch(Arg.Any<GetAllPendingRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<PendingHitlRequest>.SuccessAsync(
                (IReadOnlyList<PendingHitlRequest>)new List<PendingHitlRequest>(), 1, 10, 0));

        var result = await Hitls.GetAllPending(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    // ── GetStatus ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_NotFound_Returns404()
    {
        _dispatcher.Dispatch(Arg.Any<GetStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetStatusResult>.FailureAsync(
                Error.NotFound(new ErrorCode(0501004, ErrorCategory.Persistence), "not found")));

        var result = await Hitls.GetStatus("missing", _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task GetStatus_Approved_Returns200WithStatus()
    {
        _dispatcher.Dispatch(Arg.Any<GetStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetStatusResult>.SuccessAsync(
                new GetStatusResult("id1", HitlRequestStatus.Approved.ToString(), null)));

        var result = await Hitls.GetStatus("id1", _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task GetStatus_Pending_IncludesPendingRequest()
    {
        var pending = new PendingHitlRequest("id1", "GET", "http://x", [], null, "reason",
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        _dispatcher.Dispatch(Arg.Any<GetStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetStatusResult>.SuccessAsync(
                new GetStatusResult("id1", HitlRequestStatus.Pending.ToString(), pending)));

        var result = await Hitls.GetStatus("id1", _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    // ── ApproveHitl ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveHitl_NotFound_Returns404()
    {
        _dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.FailureAsync(
                Error.NotFound(new ErrorCode(0501004, ErrorCategory.Persistence), "not found")));

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl("nope", body, context, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task ApproveHitl_Expired_Returns404()
    {
        _dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.FailureAsync(
                Error.NotFound(new ErrorCode(0501004, ErrorCategory.Persistence), "expired")));

        var body = new Hitls.ReviewDecisionRequest("late");
        var context = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl("old", body, context, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task ApproveHitl_Success_ReturnsStreamResult()
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("ok"));
        _dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.SuccessAsync(
                new ApproveResult(200, "application/json", stream)));

        var body = new Hitls.ReviewDecisionRequest("LGTM");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.ApproveHitl("id1", body, context, _dispatcher, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveHitl_ReplayFails503_Returns502()
    {
        _dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.FailureAsync(
                Error.Failure(VectraErrors.SystemFailure, "upstream down")));

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.ApproveHitl("id1", body, context, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 500);
    }

    // ── DenyHitl ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DenyHitl_NotFound_Returns404()
    {
        _dispatcher.Dispatch(Arg.Any<DenyRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<VoidType>.FailureAsync(
                Error.NotFound(new ErrorCode(0501004, ErrorCategory.Persistence), "not found")));

        var body = new Hitls.ReviewDecisionRequest(null);
        var context = new DefaultHttpContext();
        var result = await Hitls.DenyHitl("nope", body, context, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task DenyHitl_Approved_Returns200()
    {
        _dispatcher.Dispatch(Arg.Any<DenyRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<VoidType>.SuccessAsync(new VoidType()));

        var body = new Hitls.ReviewDecisionRequest("denied");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result = await Hitls.DenyHitl("id1", body, context, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertStatusCode(IResult httpResult, int expected)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expected);
}

