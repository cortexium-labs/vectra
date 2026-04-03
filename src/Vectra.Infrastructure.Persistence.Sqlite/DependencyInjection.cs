using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Core.Interfaces;
using Vectra.Infrastructure.Persistence.Abstractions;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Repositories;
using Vectra.Infrastructure.Persistence.Sqlite.Services;

namespace Vectra.Infrastructure.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services, DatabaseConnection databaseConnection)
    {
        services
            .AddScoped<IAgentRepository, AgentRepository>()
            .AddScoped<IPolicyRepository, PolicyRepository>()
            .AddScoped<IAuditRepository, AuditRepository>()
            .AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>()
            .AddPooledDbContextFactory<SqliteApplicationContext>(options =>
            {
                options.UseSqlite(databaseConnection.ConnectionString);
            });

        return services;
    }
}