using Vectra.Domain.Policies;

namespace Vectra.Application.Abstractions.Executions;

public interface IPolicyLoader
{
    Task<Dictionary<Guid, PolicyDefinition>> LoadAllAsync(CancellationToken ct = default);
}