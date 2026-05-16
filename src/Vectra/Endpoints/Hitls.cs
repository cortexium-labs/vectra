using Microsoft.AspNetCore.Mvc;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Extensions;
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
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.GetAllPendingHitl(page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }

    public static async Task<IResult> GetStatus(
        string id,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.GetHitlStatus(id, cancellationToken);
        if (!result.IsSuccess)
            return result.ToHttpResult();

        var value = result.Value!;
        return Results.Ok(new HitlStatusResponse(value.Id, value.Status, value.Request));
    }

    public static async Task<IResult> ApproveHitl(
        string id,
        [FromBody] ReviewDecisionRequest body,
        HttpContext httpContext,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var reviewerId = httpContext.User.Identity?.Name ?? "unknown";
        var result = await dispatcher.ApproveHitl(id, reviewerId, body.Comment, cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        var value = result.Value!;
        return new UpstreamStreamResult(value.ResponseBody, value.ContentType, value.StatusCode);
    }

    public static async Task<IResult> DenyHitl(
        string id,
        [FromBody] ReviewDecisionRequest body,
        HttpContext httpContext,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var reviewerId = httpContext.User.Identity?.Name ?? "unknown";
        var result = await dispatcher.DenyHitl(id, reviewerId, body.Comment, cancellationToken);
        return result.ToHttpResult();
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
