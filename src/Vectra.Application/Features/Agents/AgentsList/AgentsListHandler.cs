using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Agents.AgentsList;

internal class AgentsListHandler : IActionHandler<AgentsListRequest, PaginatedResult<AgentsListResult>>
{
    private readonly ILogger<AgentsListHandler> _logger;
    private readonly IAgentRepository _agentRepository;

    public AgentsListHandler(
        ILogger<AgentsListHandler> logger,
        IAgentRepository agentRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
    }

    public async Task<PaginatedResult<AgentsListResult>> Handle(AgentsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (agents, totalCount) = await _agentRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

            var items = agents.Select(a => new AgentsListResult
            {
                AgentId = a.Id,
                Name = a.Name,
                OwnerId = a.OwnerId,
                Status = a.Status,
                PolicyName = a.PolicyName
            }).ToList();

            return await PaginatedResult<AgentsListResult>.SuccessAsync(items, request.Page, request.PageSize, totalCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("AgentsList request was cancelled by the client.");
            return await PaginatedResult<AgentsListResult>.FailureAsync(
                Error.Failure(ApplicationErrorCodes.RequestCancelled, "The request was cancelled."));
        }
    }
}