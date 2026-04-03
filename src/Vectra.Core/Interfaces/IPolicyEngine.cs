namespace Vectra.Core.Interfaces;

public interface IPolicyEngine
{
    Task<OpaDecision> EvaluateAsync(string package, object input, CancellationToken cancellationToken = default);
}

public record OpaDecision(string Decision);