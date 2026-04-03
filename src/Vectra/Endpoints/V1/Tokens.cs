using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Vectra.Core.DTOs;
using Vectra.Core.UseCases;
using Vectra.Extensions;

namespace Vectra.Endpoints.V1;

public class Tokens : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/v{version:apiVersion}/Tokens")
            .HasApiVersion(new ApiVersion(1, 0))
            .WithTags("Authentication");

        group.MapPost("", GetToken)
            .WithName("GetToken")
            .WithSummary("Exchange credentials for JWT")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    public static async Task<IResult> GetToken(
        [FromBody] TokenRequest request,
        AuthenticateAgentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var token = await useCase.ExecuteAsync(request, cancellationToken);
        if (token == null)
            return Results.Unauthorized();
        return Results.Ok(new { access_token = token });
    }

    public record TokenResponse(string access_token);
}