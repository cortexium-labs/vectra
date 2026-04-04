using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vectra.Core.Entities;
using Vectra.Infrastructure.Persistence.Abstractions.Exceptions;

namespace Vectra.Infrastructure.Persistence.Abstractions;

public abstract class BaseDbContext : DbContext, IDatabaseContext
{
    protected readonly ILogger<BaseDbContext> Logger;

    protected BaseDbContext(
            DbContextOptions options,
            ILogger<BaseDbContext> logger): base(options)
    {
        Logger = logger;
    }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<PolicyDefinition> Policies { get; set; }
    public DbSet<PolicyRule> Rules { get; set; }
    public DbSet<RuleCondition> Conditions { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DatabaseSaveException(ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);
        }
        catch (Exception ex)
        {
            throw new DatabaseModelCreatingException(ex);
        }
    }
}