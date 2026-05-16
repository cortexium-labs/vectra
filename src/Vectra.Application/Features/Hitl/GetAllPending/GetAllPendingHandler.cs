using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Hitl.GetAllPending;

internal class GetAllPendingHandler : IActionHandler<GetAllPendingRequest, PaginatedResult<PendingHitlRequest>>
{
    private readonly ILogger<GetAllPendingHandler> _logger;
    private readonly IHitlService _hitlService;

    public GetAllPendingHandler(ILogger<GetAllPendingHandler> logger, IHitlService hitlService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hitlService = hitlService ?? throw new ArgumentNullException(nameof(hitlService));
    }

    public async Task<PaginatedResult<PendingHitlRequest>> Handle(GetAllPendingRequest request, CancellationToken cancellationToken = default)
    {
        var (pendings, totalCount) = await _hitlService.GetAllPendingPagedAsync(request.Page, request.PageSize, cancellationToken);
        return await PaginatedResult<PendingHitlRequest>.SuccessAsync(pendings, request.Page, request.PageSize, totalCount);
    }
}
