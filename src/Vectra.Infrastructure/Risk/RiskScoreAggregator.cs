using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk;

public class RiskScoreAggregator
{
    private readonly IEnumerable<IRiskCalculator> _calculators;

    public RiskScoreAggregator(IEnumerable<IRiskCalculator> calculators)
    {
        _calculators = calculators;
    }

    public async Task<double> AggregateAsync(
        RequestContext context,
        AgentHistory? history,
        CancellationToken cancellationToken)
    {
        var calculators = _calculators.ToList();

        var tasks = calculators
            .Select(c => c.CalculateAsync(context, history, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        double totalWeight = 0;
        double weightedSum = 0;

        for (int i = 0; i < calculators.Count; i++)
        {
            var calc = calculators[i];
            totalWeight += calc.Weight;
            weightedSum += results[i] * calc.Weight;
        }

        const double epsilon = 1e-9;
        if (Math.Abs(totalWeight) < epsilon)
            return 0;

        var finalScore = weightedSum / totalWeight;

        return Math.Clamp(finalScore, 0, 1);
    }
}