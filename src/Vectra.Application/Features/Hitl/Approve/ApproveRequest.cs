using Vectra.Application.Abstractions.Dispatchers;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.Approve;

public class ApproveRequest : IRequest<Result<ApproveResult>>
{
    public required string Id { get; set; }
    public required string ReviewerId { get; set; }
    public string? Comment { get; set; }
}
