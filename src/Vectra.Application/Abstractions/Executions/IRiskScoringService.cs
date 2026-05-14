using Vectra.Application.Models;

namespace Vectra.Application.Abstractions.Executions;

public interface IRiskScoringService
{
    Task<double> ComputeRiskScoreAsync(RequestContext context, CancellationToken cancellationToken = default);
}