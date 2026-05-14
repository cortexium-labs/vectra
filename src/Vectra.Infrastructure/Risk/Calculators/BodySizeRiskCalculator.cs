using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk.Calculators;

public class BodySizeRiskCalculator : IRiskCalculator
{
    public string Name => "body_size";
    public double Weight { get; set; } = 0.1;

    public Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.Body)) return Task.FromResult(0.0);
        var size = context.Body.Length;
        if (size > 1024 * 1024) return Task.FromResult(0.8);      // >1MB
        if (size > 100 * 1024) return Task.FromResult(0.5);       // >100KB
        if (size > 10 * 1024) return Task.FromResult(0.2);        // >10KB
        return Task.FromResult(0.0);
    }
}