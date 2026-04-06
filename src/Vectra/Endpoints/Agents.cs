using Microsoft.AspNetCore.Mvc;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Extensions;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.Extensions;

namespace Vectra.Endpoints;

public class Agents : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this).WithTags("Agents");

        group.MapPost("", RegisterAgent)
            .WithName("RegisterAgent")
            .WithSummary("Register a new AI agent");

        group.MapPut("/{agentId}/policy", AssignPolicyToAgent)
            .WithName("AssignPolicyToAgent")
            .WithSummary("Assign a policy to an AI agent");
    }

    public static async Task<IResult> RegisterAgent(
        [FromBody] CreateAgentRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.RegisterAgent(request, cancellationToken);
        return result.ToHttpResult();
    }

    public static async Task<IResult> AssignPolicyToAgent(
        string agentId,
        [FromBody] AssignPolicyRequestModel request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.AssignPolicyToAgent(agentId, request.PolicyId, cancellationToken);
        return result.ToHttpResult();
    }
}