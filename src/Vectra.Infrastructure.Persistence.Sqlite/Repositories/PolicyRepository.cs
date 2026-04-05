using Microsoft.EntityFrameworkCore;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Vectra.Infrastructure.Persistence.Sqlite.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly IPolicyCacheInvalidator _policyCacheInvalidator;

    public PolicyRepository(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        IPolicyCacheInvalidator policyCacheInvalidator)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _policyCacheInvalidator = policyCacheInvalidator ?? throw new ArgumentNullException(nameof(policyCacheInvalidator));
    }

    public async Task AddAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Policies.AddAsync(policy, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _policyCacheInvalidator.InvalidateAsync(policy.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await context.Policies.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (policy != null)
        {
            context.Policies.Remove(policy);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _policyCacheInvalidator.InvalidateAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PolicyDefinition?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Policies
            .Include(p => p.Rules).ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PolicyDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Policies
            .Include(p => p.Rules).ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(policy).State = EntityState.Detached;
        context.Policies.Update(policy);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _policyCacheInvalidator.InvalidateAsync(policy.Id, cancellationToken).ConfigureAwait(false);
    }
}