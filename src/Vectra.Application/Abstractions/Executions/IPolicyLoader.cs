using Vectra.Domain.Policies;

namespace Vectra.Application.Abstractions.Executions;

public interface IPolicyLoader
{
    Task<PolicyDefinition?> GetPolicyAsync(Guid policyId, CancellationToken ct = default);
    Task<Dictionary<Guid, PolicyDefinition>> LoadAllAsync(CancellationToken ct = default);
}