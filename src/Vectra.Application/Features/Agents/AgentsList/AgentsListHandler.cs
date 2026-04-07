using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Persistence;
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
        var (agents, totalCount) = await _agentRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

        var items = agents.Select(a => new AgentsListResult
        {
            AgentId = a.Id,
            Name = a.Name,
            OwnerId = a.OwnerId,
            Status = a.Status,
            PolicyId = a.PolicyId
        }).ToList();

        return PaginatedResult<AgentsListResult>.Success(items, request.Page, request.PageSize, totalCount);
    }
}