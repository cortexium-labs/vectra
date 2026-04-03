using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vectra.Infrastructure.Persistence.Abstractions;
using Vectra.Infrastructure.Persistence.Abstractions.Exceptions;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Vectra.Infrastructure.Persistence.Sqlite.Services;

public class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<SqliteApplicationContext> _contextFactory;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;

    public SqliteDatabaseInitializer(
        IDbContextFactory<SqliteApplicationContext> contextFactory,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Database.EnsureCreatedAsync(cancellationToken);

            if (result)
                _logger.LogInformation("Application database created successfully (SQLite).");
            else
                _logger.LogInformation("Application database already exists (SQLite).");
        }
        catch (Exception ex)
        {
            throw new DatabaseInitializerException(ex);
        }
    }
}