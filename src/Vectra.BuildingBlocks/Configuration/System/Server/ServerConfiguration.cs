namespace Vectra.BuildingBlocks.Configuration.System.Server;

public class ServerConfiguration
{
    public HttpServerConfiguration? Http { get; set; }
    public HttpsServerConfiguration? Https { get; set; }
    public int? MaxConcurrentConnections { get; set; } = 1000;
    public int? MaxConcurrentUpgradedConnections { get; set; } = 1000;
    public TimeSpan? KeepAliveTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan? RequestHeadersTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int? MaxRequestBodySizeMb { get; set; } = 50;
}