using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk.Calculators;

public class AgentHistoryCalculator : IRiskCalculator
{
    public string Name => "agent_history";
    public double Weight { get; set; } = 0.35;

    private readonly IAgentHistoryRepository _historyRepo;
    public AgentHistoryCalculator(IAgentHistoryRepository historyRepo)
    {
        _historyRepo = historyRepo;
    }

    public async Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken)
    {
        // Use provided history if already loaded (to avoid extra DB call)
        if (history == null)
            history = await _historyRepo.GetRecentAsync(context.AgentId, TimeSpan.FromMinutes(5), cancellationToken);
        if (history == null) return 0.3; // unknown

        double risk = 0.0;
        // Factor 1: violation rate in last 5 minutes
        if (history.TotalRequests > 0)
        {
            var violationRate = history.ViolationCount / (double)history.TotalRequests;
            risk += violationRate * 0.5;
        }
        // Factor 2: request frequency (too high = risky)
        var rpm = history.TotalRequests / 5.0; // requests per minute (over 5 min window)
        if (rpm > 60) risk += 0.3;
        else if (rpm > 30) risk += 0.15;
        else if (rpm > 10) risk += 0.05;

        // Factor 3: trust score decay (if agent trust score is low, increase risk)
        if (context.TrustScore < 0.3) risk += 0.4;
        else if (context.TrustScore < 0.6) risk += 0.2;

        return Math.Min(1.0, risk);
    }
}