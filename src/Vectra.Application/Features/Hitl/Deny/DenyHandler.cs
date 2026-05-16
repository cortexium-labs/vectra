using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.Deny;

internal class DenyHandler : IActionHandler<DenyRequest, Result<Abstractions.Dispatchers.Void>>
{
    private readonly IHitlService _hitlService;

    public DenyHandler(IHitlService hitlService)
    {
        _hitlService = hitlService ?? throw new ArgumentNullException(nameof(hitlService));
    }

    public async Task<Result<Abstractions.Dispatchers.Void>> Handle(DenyRequest request, CancellationToken cancellationToken = default)
    {
        var status = await _hitlService.GetStatusAsync(request.Id, cancellationToken);
        if (status == HitlRequestStatus.NotFound || status == HitlRequestStatus.Expired)
            return await Result<Abstractions.Dispatchers.Void>.FailureAsync(
                Error.NotFound(ApplicationErrorCodes.HitlRequestNotFound, $"HITL request '{request.Id}' was not found or has expired."));

        await _hitlService.DenyAsync(request.Id, request.ReviewerId, request.Comment, cancellationToken);
        return await Result<Abstractions.Dispatchers.Void>.SuccessAsync(new Abstractions.Dispatchers.Void());
    }
}
