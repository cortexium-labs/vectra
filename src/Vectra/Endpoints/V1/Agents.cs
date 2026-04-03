using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Vectra.BuildingBlocks.Services;
using Vectra.Core.DTOs;
using Vectra.Core.UseCases;
using Vectra.Extensions;

namespace Vectra.Endpoints.V1;

public class Agents : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/v{version:apiVersion}/Agents")
            .HasApiVersion(new ApiVersion(1, 0))
            .WithTags("Agents");

        group.MapPost("", RegisterAgent)
            .WithName("RegisterAgent")
            .WithSummary("Register a new AI agent")
            .Produces<RegisterResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    public static async Task<IResult> RegisterAgent(
        [FromBody] RegisterAgentRequest request,
        RegisterAgentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var agentId = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Ok(new { agent_id = agentId });
    }

    public record RegisterResponse(Guid agent_id);
}