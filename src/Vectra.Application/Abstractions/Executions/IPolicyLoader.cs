using Vectra.Domain.Policies;

namespace Vectra.Application.Abstractions.Executions;

public interface IPolicyLoader
{
    Task<PolicyDefinition?> GetPolicyAsync(string policyName, CancellationToken ct = default);
    Task<Dictionary<string, PolicyDefinition>> LoadAllAsync(CancellationToken ct = default);
}