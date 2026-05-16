using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Agents.AgentsList;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.DeleteAgent;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.Application.Features.Hitl.Approve;
using Vectra.Application.Features.Hitl.Deny;
using Vectra.Application.Features.Hitl.GetAllPending;
using Vectra.Application.Features.Hitl.GetStatus;
using Vectra.Application.Features.Policies.PolicyDetails;
using Vectra.Application.Features.Policies.PoliciesList;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Extensions;

public static class DispatcherExtensions
{
    #region Agents

    public static Task<PaginatedResult<AgentsListResult>> AgentsList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AgentsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

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
        string policyName,
        CancellationToken cancellationToken)
    {
        var request = new AssignPolicyRequest
        {
            AgentId = agentId,
            PolicyName = policyName
        };
        return dispatcher.Dispatch(request, cancellationToken);
    }

    public static Task<Result<Abstractions.Dispatchers.Void>> DeleteAgent(
        this IDispatcher dispatcher,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var request = new DeleteAgentRequest { AgentId = agentId.ToString() };
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

    #region Policies

    public static Task<PaginatedResult<PoliciesListResult>> PoliciesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new PoliciesListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<PolicyDetailsResult>> PolicyDetails(
        this IDispatcher dispatcher,
        string name,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new PolicyDetailsRequest { Name = name }, cancellationToken);
    }

    #endregion

    #region Hitl

    public static Task<PaginatedResult<Abstractions.Executions.PendingHitlRequest>> GetAllPendingHitl(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GetAllPendingRequest { Page = page, PageSize = pageSize }, cancellationToken);
    }

    public static Task<Result<GetStatusResult>> GetHitlStatus(
        this IDispatcher dispatcher,
        string id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GetStatusRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ApproveResult>> ApproveHitl(
        this IDispatcher dispatcher,
        string id,
        string reviewerId,
        string? comment,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ApproveRequest { Id = id, ReviewerId = reviewerId, Comment = comment }, cancellationToken);
    }

    public static Task<Result<Abstractions.Dispatchers.Void>> DenyHitl(
        this IDispatcher dispatcher,
        string id,
        string reviewerId,
        string? comment,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DenyRequest { Id = id, ReviewerId = reviewerId, Comment = comment }, cancellationToken);
    }

    #endregion
}
