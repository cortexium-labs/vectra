using Vectra.Application.Abstractions.Dispatchers;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.GetStatus;

public class GetStatusRequest : IRequest<Result<GetStatusResult>>
{
    public required string Id { get; set; }
}
