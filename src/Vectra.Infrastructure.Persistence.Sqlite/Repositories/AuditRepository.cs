//using Microsoft.EntityFrameworkCore;
//using Vectra.Core.Entities;
//using Vectra.Core.Interfaces;
//using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

//namespace Vectra.Infrastructure.Persistence.Sqlite.Repositories;

//public class AuditRepository : IAuditRepository
//{
//    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

//    public AuditRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
//    {
//        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
//    }

//    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
//    {
//        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
//        await context.AuditLogs.AddAsync(log, cancellationToken).ConfigureAwait(false);
//        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
//    }
//}