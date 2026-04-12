namespace Vectra.BuildingBlocks.Configuration.System.Storage.Cache;

public class RedisCacheConfiguration
{
    public string Address { get; set; } = default!;
    public TimeSpan? TimeToLive { get; set; } = TimeSpan.FromHours(24);
    public bool? AbortOnConnectFail { get; set; } = false;
    public int? ConnectRetry { get; set; } = 5;
    public TimeSpan? ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
}