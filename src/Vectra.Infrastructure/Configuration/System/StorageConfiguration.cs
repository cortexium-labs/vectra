using Vectra.Infrastructure.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Configuration.System.Storage.Database;

namespace Vectra.Infrastructure.Configuration.System;

public class StorageConfiguration
{
    public DatabaseConfiguration Database { get; set; } = new();
    public CacheConfiguration Cache { get; set; } = new();
}