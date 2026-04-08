using Vectra.Domain.Agents;

namespace Vectra.Application.Abstractions.Persistence;

public interface IAgentRepository
{
    Task<(IReadOnlyList<Agent> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Agent agent, CancellationToken cancellationToken = default);

    Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
