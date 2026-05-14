using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk.Calculators;

public class MethodRiskCalculator : IRiskCalculator
{
    public string Name => "method";
    public double Weight { get; set; } = 0.2;

    private static readonly Dictionary<string, double> MethodRisk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GET"] = 0.1,
        ["HEAD"] = 0.05,
        ["OPTIONS"] = 0.05,
        ["POST"] = 0.4,
        ["PUT"] = 0.6,
        ["PATCH"] = 0.5,
        ["DELETE"] = 0.9,
        ["TRACE"] = 0.7,
        ["CONNECT"] = 0.8
    };

    public Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken)
    {
        var risk = MethodRisk.TryGetValue(context.Method, out var value) ? value : 0.5;
        return Task.FromResult(risk);
    }
}