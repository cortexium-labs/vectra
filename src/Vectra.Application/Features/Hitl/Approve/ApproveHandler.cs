using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.Approve;

internal class ApproveHandler : IActionHandler<ApproveRequest, Result<ApproveResult>>
{
    private readonly IHitlService _hitlService;

    public ApproveHandler(IHitlService hitlService)
    {
        _hitlService = hitlService ?? throw new ArgumentNullException(nameof(hitlService));
    }

    public async Task<Result<ApproveResult>> Handle(ApproveRequest request, CancellationToken cancellationToken = default)
    {
        var status = await _hitlService.GetStatusAsync(request.Id, cancellationToken);
        if (status == HitlRequestStatus.NotFound || status == HitlRequestStatus.Expired)
            return await Result<ApproveResult>.FailureAsync(
                Error.NotFound(ApplicationErrorCodes.HitlRequestNotFound, $"HITL request '{request.Id}' was not found or has expired."));

        await _hitlService.ApproveAsync(request.Id, request.ReviewerId, request.Comment, cancellationToken);

        var replayResult = await _hitlService.ReplayAsync(request.Id, cancellationToken);

        if (!replayResult.Success)
        {
            var errorCode = replayResult.StatusCode == 503
                ? VectraErrors.SystemFailure
                : ApplicationErrorCodes.HitlRequestNotFound;
            return await Result<ApproveResult>.FailureAsync(
                Error.Failure(errorCode, replayResult.ErrorReason ?? "Replay failed."));
        }

        var contentType = replayResult.ResponseHeaders?.GetValueOrDefault("Content-Type") ?? "application/octet-stream";
        return await Result<ApproveResult>.SuccessAsync(
            new ApproveResult(replayResult.StatusCode ?? 200, contentType, replayResult.ResponseBody!));
    }
}
