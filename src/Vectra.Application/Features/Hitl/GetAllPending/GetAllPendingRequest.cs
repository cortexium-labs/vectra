using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.GetAllPending;

public class GetAllPendingRequest : IRequest<PaginatedResult<PendingHitlRequest>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}