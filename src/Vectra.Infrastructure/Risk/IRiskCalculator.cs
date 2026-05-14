using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk;

public interface IRiskCalculator
{
    string Name { get; }
    double Weight { get; }  // configurable weight
    Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken);
}