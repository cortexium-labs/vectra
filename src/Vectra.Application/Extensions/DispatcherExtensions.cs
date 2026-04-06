using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Extensions;

public static class DispatcherExtensions
{
    #region Agents

    public static Task<Result<CreateAgentResult>> RegisterAgent(
        this IDispatcher dispatcher,
        CreateAgentRequest request,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(request, cancellationToken);
    }

    public static Task<Result<Abstractions.Dispatchers.Void>> AssignPolicyToAgent(
        this IDispatcher dispatcher,
        string agentId,
        string policyId,
        CancellationToken cancellationToken)
    {
        var request = new AssignPolicyRequest
        {
            AgentId = agentId,
            PolicyId = policyId
        };
        return dispatcher.Dispatch(request, cancellationToken);
    }

    #endregion

    #region Authentication

    public static Task<Result<GenerateTokenResult>> GenerateToken(
        this IDispatcher dispatcher,
        GenerateTokenRequest request,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(request, cancellationToken);
    }

    #endregion
}
