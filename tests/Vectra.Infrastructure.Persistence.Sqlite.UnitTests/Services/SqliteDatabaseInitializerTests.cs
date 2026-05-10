using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Vectra.Infrastructure.Persistence.Common.Exceptions;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Services;
using Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Helpers;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Services;

public class SqliteDatabaseInitializerTests
{
    [Fact]
    public async Task EnsureDatabaseCreatedAsync_NewDatabase_LogsCreated()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = SqliteTestContextFactory.CreateFactory(dbName);

        var initializer = new SqliteDatabaseInitializer(factory, NullLogger<SqliteDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_ExistingDatabase_DoesNotThrow()
    {
        var dbName = Guid.NewGuid().ToString();

        // First call creates the DB
        var factory1 = SqliteTestContextFactory.CreateFactory(dbName);
        await new SqliteDatabaseInitializer(factory1, NullLogger<SqliteDatabaseInitializer>.Instance)
            .EnsureDatabaseCreatedAsync();

        // Second call should still succeed
        var factory2 = SqliteTestContextFactory.CreateFactory(dbName);
        var initializer2 = new SqliteDatabaseInitializer(factory2, NullLogger<SqliteDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer2.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_FactoryThrows_ThrowsDatabaseInitializerException()
    {
        var factory = new ThrowingContextFactory();
        var initializer = new SqliteDatabaseInitializer(factory, NullLogger<SqliteDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DatabaseInitializerException>();
    }

    private sealed class ThrowingContextFactory : IDbContextFactory<SqliteApplicationContext>
    {
        public SqliteApplicationContext CreateDbContext() => throw new InvalidOperationException("Connection failed");
        public Task<SqliteApplicationContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Connection failed");
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_WithCancellationToken_DoesNotThrow()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = SqliteTestContextFactory.CreateFactory(dbName);

        var initializer = new SqliteDatabaseInitializer(factory, NullLogger<SqliteDatabaseInitializer>.Instance);
        using var cts = new CancellationTokenSource();

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }
}
