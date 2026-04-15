using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Persistence;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.Infrastructure.Persistence.Common;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Repositories;
using Vectra.Infrastructure.Persistence.Sqlite.Services;

namespace Vectra.Infrastructure.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services)
    {
        services
            .AddScoped<IAgentRepository, AgentRepository>()
            .AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>()
            .AddScoped<IAgentHistoryRepository, AgentHistoryRepository>();

        services.AddPooledDbContextFactory<SqliteApplicationContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<IOptions<SystemConfiguration>>()
                       .Value.Storage.Database;

            options.UseSqlite(db.Providers.Sqlite.ConnectionString);
        });

        return services;
    }
}