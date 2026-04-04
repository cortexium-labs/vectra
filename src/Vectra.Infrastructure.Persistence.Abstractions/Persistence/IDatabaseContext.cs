using Microsoft.EntityFrameworkCore;
using Vectra.Core.Entities;

namespace Vectra.Infrastructure.Persistence.Abstractions;

public interface IDatabaseContext
{
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<PolicyDefinition> Policies { get; set; }
    public DbSet<PolicyRule> Rules { get; set; }
    public DbSet<RuleCondition> Conditions { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}