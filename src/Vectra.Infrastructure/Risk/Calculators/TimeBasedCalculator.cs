using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Risk.Calculators;

public class TimeBasedCalculator : IRiskCalculator
{
    public string Name => "time_of_day";
    public double Weight { get; set; } = 0.1;

    public Task<double> CalculateAsync(RequestContext context, AgentHistory? history, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        var isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

        double risk = 0.0;
        if (isWeekend) risk += 0.2;
        if (hour < 6 || hour > 20) risk += 0.3; // night time
        else if (hour < 8 || hour > 18) risk += 0.1; // early morning / late evening

        return Task.FromResult(Math.Min(0.5, risk));
    }
}
