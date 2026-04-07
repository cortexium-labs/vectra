using Vectra.Application.Abstractions.Dispatchers;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Agents.AgentsList;

public class AgentsListRequest : IRequest<PaginatedResult<AgentsListResult>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}