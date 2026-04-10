using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Infrastructure.Persistence.Common;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Repositories;
using Vectra.Infrastructure.Persistence.Sqlite.Services;

namespace Vectra.Infrastructure.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services, string databaseConnection)
    {
        services
            .AddScoped<IAgentRepository, AgentRepository>()
            .AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>()
            .AddPooledDbContextFactory<SqliteApplicationContext>(options =>
            {
                options.UseSqlite(databaseConnection);
            });

        return services;
    }
}