using System.Text.RegularExpressions;
using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk.Calculators;

public class PathRiskCalculator : IRiskCalculator
{
    public string Name => "path";
    public double Weight { get; set; } = 0.25;

    private static readonly List<(Regex Pattern, double Risk)> PathPatterns = new()
    {
        (new Regex(@"/admin/", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.8),
        (new Regex(@"/export|/dump|/bulk", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.9),
        (new Regex(@"/delete|/remove|/drop", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.85),
        (new Regex(@"/users/all|/users/export", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.95),
        (new Regex(@"/config|/settings|/env", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.7),
        (new Regex(@"/internal/", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.6),
        (new Regex(@"/v[0-9]+/", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3)), 0.2)  // versioned API slightly higher
    };

    public Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken)
    {
        var path = context.Path;
        double maxRisk = 0.1; // default low risk
        foreach (var (pattern, risk) in PathPatterns)
        {
            if (pattern.IsMatch(path))
            {
                maxRisk = Math.Max(maxRisk, risk);
            }
        }
        return Task.FromResult(maxRisk);
    }
}