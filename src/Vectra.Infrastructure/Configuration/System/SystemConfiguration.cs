using Vectra.Infrastructure.Configuration.System.Server;
using Vectra.Infrastructure.Configuration.System.Storage.Cache;
using Vectra.Infrastructure.Configuration.System.Storage.Database;

namespace Vectra.Infrastructure.Configuration.System;

public class SystemConfiguration
{
    public ServerConfiguration Server { get; set; } = new();
    public StorageConfiguration Storage { get; set; } = new();
}