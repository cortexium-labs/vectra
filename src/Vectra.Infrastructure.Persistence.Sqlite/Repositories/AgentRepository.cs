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
        context.Entry(agent).State = EntityState.Detached;
        context.Agents.Update(agent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}