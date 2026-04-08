using Microsoft.EntityFrameworkCore;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Vectra.Infrastructure.Persistence.Sqlite.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public AgentRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<(IReadOnlyList<Agent> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

        var totalCount = await context.Agents
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await context.Agents
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Agents.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Agents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Agents.AddAsync(agent, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Agents.Update(agent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var agent = await context.Agents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false) 
            ?? throw new InvalidOperationException($"Agent with ID {id} does not exist.");

        context.Agents.Remove(agent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}