using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.GetStatus;

internal class GetStatusHandler : IActionHandler<GetStatusRequest, Result<GetStatusResult>>
{
    private readonly IHitlService _hitlService;

    public GetStatusHandler(IHitlService hitlService)
    {
        _hitlService = hitlService ?? throw new ArgumentNullException(nameof(hitlService));
    }

    public async Task<Result<GetStatusResult>> Handle(GetStatusRequest request, CancellationToken cancellationToken = default)
    {
        var status = await _hitlService.GetStatusAsync(request.Id, cancellationToken);

        if (status == HitlRequestStatus.NotFound)
            return await Result<GetStatusResult>.FailureAsync(
                Error.NotFound(ApplicationErrorCodes.HitlRequestNotFound, $"HITL request '{request.Id}' was not found."));

        PendingHitlRequest? pending = null;
        if (status == HitlRequestStatus.Pending)
            pending = await _hitlService.GetPendingAsync(request.Id, cancellationToken);

        return await Result<GetStatusResult>.SuccessAsync(new GetStatusResult(request.Id, status.ToString(), pending));
    }
}
