using Vectra.Application.Abstractions.Executions;
using Vectra.Extensions;

namespace Vectra.Endpoints;

public class Hitls : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this).WithTags("Human-in-the-Loop");

        group.MapGet("", GetAllPending)
            .WithName("GetAllPendingHitl")
            .WithSummary("List all pending HITL requests")
            .Produces<IReadOnlyList<PendingHitlRequest>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", GetStatus)
            .WithName("GetHitlStatus")
            .WithSummary("Get full status and details of a HITL request")
            .Produces<HitlStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/approve", ApproveHitl)
            .WithName("ApproveHitl")
            .WithSummary("Approve a pending HITL request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/deny", DenyHitl)
            .WithName("DenyHitl")
            .WithSummary("Deny a pending HITL request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static async Task<IResult> GetAllPending(
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        var pending = await hitlService.GetAllPendingAsync(cancellationToken);
        return Results.Ok(pending);
    }

    public static async Task<IResult> GetStatus(
        string id,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        var status = await hitlService.GetStatusAsync(id, cancellationToken);

        if (status == HitlRequestStatus.NotFound)
            return Results.NotFound();

        PendingHitlRequest? pending = null;
        if (status == HitlRequestStatus.Pending)
            pending = await hitlService.GetPendingAsync(id, cancellationToken);

        return Results.Ok(new HitlStatusResponse(id, status.ToString(), pending));
    }

    public static async Task<IResult> ApproveHitl(
        string id,
        ReviewDecisionRequest body,
        HttpContext httpContext,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        var status = await hitlService.GetStatusAsync(id, cancellationToken);
        if (status == HitlRequestStatus.NotFound || status == HitlRequestStatus.Expired)
            return Results.NotFound();

        var reviewerId = httpContext.User.Identity?.Name ?? "unknown";
        await hitlService.ApproveAsync(id, reviewerId, body.Comment, cancellationToken);

        var result = await hitlService.ReplayAsync(id, cancellationToken);

        if (!result.Success)
        {
            if (result.StatusCode == 503)
                return Results.Problem(result.ErrorReason, statusCode: StatusCodes.Status502BadGateway);
            return Results.BadRequest(result.ErrorReason);
        }

        var contentType = result.ResponseHeaders?.GetValueOrDefault("Content-Type") ?? "application/octet-stream";
        return new UpstreamStreamResult(result.ResponseBody!, contentType, result.StatusCode ?? 200);
    }

    public static async Task<IResult> DenyHitl(
        string id,
        ReviewDecisionRequest body,
        HttpContext httpContext,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        var status = await hitlService.GetStatusAsync(id, cancellationToken);
        if (status == HitlRequestStatus.NotFound || status == HitlRequestStatus.Expired)
            return Results.NotFound();

        var reviewerId = httpContext.User.Identity?.Name ?? "unknown";
        await hitlService.DenyAsync(id, reviewerId, body.Comment, cancellationToken);
        return Results.Ok();
    }

    public record HitlStatusResponse(string Id, string Status, PendingHitlRequest? Request);
    public record ReviewDecisionRequest(string? Comment);

    private sealed class UpstreamStreamResult : IResult
    {
        private readonly Stream _body;
        private readonly string _contentType;
        private readonly int _statusCode;

        public UpstreamStreamResult(Stream body, string contentType, int statusCode)
        {
            _body = body;
            _contentType = contentType;
            _statusCode = statusCode;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = _statusCode;
            httpContext.Response.ContentType = _contentType;
            await _body.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted);
        }
    }
}