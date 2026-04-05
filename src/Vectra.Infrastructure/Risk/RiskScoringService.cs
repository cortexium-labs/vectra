using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Models;

namespace Vectra.Infrastructure.Risk;

public class RiskScoringService : IRiskScoringService
{
    public double ComputeRiskScore(RequestContext context)
    {
        double score = 0.0;
        // Simple heuristics
        if (context.Method == "DELETE") score += 0.3;
        if (context.Path.Contains("/admin")) score += 0.4;
        if (context.TrustScore < 0.3) score += 0.2;
        // You can add more rules based on body content, etc.
        return Math.Clamp(score, 0, 1);
    }
}