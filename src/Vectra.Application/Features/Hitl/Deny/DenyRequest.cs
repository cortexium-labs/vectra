using Vectra.Application.Abstractions.Dispatchers;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.Deny;

public class DenyRequest : IRequest<Result<Abstractions.Dispatchers.Void>>
{
    public required string Id { get; set; }
    public required string ReviewerId { get; set; }
    public string? Comment { get; set; }
}
