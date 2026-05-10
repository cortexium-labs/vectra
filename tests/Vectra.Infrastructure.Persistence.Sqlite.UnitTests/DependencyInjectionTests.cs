using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Persistence;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.Storage.Database;
using Vectra.Infrastructure.Persistence.Common;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddSqlitePersistenceLayer_RegistersIAgentRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAgentRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddSqlitePersistenceLayer_RegistersIAgentHistoryRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAgentHistoryRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddSqlitePersistenceLayer_RegistersIAuditRepository()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IAuditRepository)).Should().BeTrue();
    }

    [Fact]
    public void AddSqlitePersistenceLayer_RegistersIDatabaseInitializer()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IDatabaseInitializer)).Should().BeTrue();
    }

    [Fact]
    public void AddSqlitePersistenceLayer_RegistersDbContextFactory()
    {
        var services = BuildServices();
        services.Any(d => d.ServiceType == typeof(IDbContextFactory<SqliteApplicationContext>)).Should().BeTrue();
    }

    [Fact]
    public void AddSqlitePersistenceLayer_IAgentRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAgentRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlitePersistenceLayer_IAgentHistoryRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAgentHistoryRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlitePersistenceLayer_IAuditRepository_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IAuditRepository));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlitePersistenceLayer_IDatabaseInitializer_IsScoped()
    {
        var services = BuildServices();
        var descriptor = services.First(d => d.ServiceType == typeof(IDatabaseInitializer));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlitePersistenceLayer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        RegisterOptions(services);

        var returned = services.AddSqlitePersistenceLayer();

        returned.Should().BeSameAs(services);
    }

    private static IServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        RegisterOptions(services);
        services.AddSqlitePersistenceLayer();
        return services;
    }

    private static void RegisterOptions(IServiceCollection services)
    {
        var config = new SystemConfiguration
        {
            Storage = new StorageConfiguration
            {
                Database = new DatabaseConfiguration
                {
                    Providers = new DatabaseProviders
                    {
                        Sqlite = new SqliteConfiguration
                        {
                            ConnectionString = "Data Source=:memory:"
                        }
                    }
                }
            }
        };

        services.AddSingleton<IOptions<SystemConfiguration>>(Options.Create(config));
        services.AddLogging();
    }
}
