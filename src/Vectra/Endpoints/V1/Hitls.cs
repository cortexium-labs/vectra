using Asp.Versioning;
using Vectra.Core.Interfaces;
using Vectra.Extensions;

namespace Vectra.Endpoints.V1;

public class Hitls : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/v{version:apiVersion}/Hitls")
            .HasApiVersion(new ApiVersion(1, 0))
            .WithTags("Human-in-the-Loop");

        group.MapGet("/{id}/status", GetStatus)
            .WithName("GetHitlStatus")
            .WithSummary("Check status of a HITL request")
            .Produces<HitlStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/approve", ApproveHitl)
            .WithName("ApproveHitl")
            .WithSummary("Approve a pending HITL request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/token", DenyHitl)
            .WithName("DenyHitl")
            .WithSummary("Deny a pending HITL request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static async Task<IResult> GetStatus(
        string id,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        var pending = await hitlService.GetPendingAsync(id, cancellationToken);
        if (pending == null)
            return Results.NotFound();
        return Results.Ok(new { status = "pending" });
    }

    public static async Task<IResult> ApproveHitl(
        string id,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        await hitlService.ApproveAsync(id, cancellationToken);
        return Results.Ok();
    }

    public static async Task<IResult> DenyHitl(
        string id,
        IHitlService hitlService,
        CancellationToken cancellationToken)
    {
        await hitlService.DenyAsync(id, cancellationToken);
        return Results.Ok();
    }

    public record HitlStatusResponse(string status);
}
