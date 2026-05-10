using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Helpers;

internal static class SqliteTestContextFactory
{
    public static SqliteApplicationContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<SqliteApplicationContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new SqliteApplicationContext(options, NullLogger<SqliteApplicationContext>.Instance);
    }

    public static IDbContextFactory<SqliteApplicationContext> CreateFactory(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        return new InMemoryContextFactory(dbName);
    }

    private sealed class InMemoryContextFactory(string databaseName)
        : IDbContextFactory<SqliteApplicationContext>
    {
        public SqliteApplicationContext CreateDbContext() => Create(databaseName);

        public Task<SqliteApplicationContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Create(databaseName));
    }
}
