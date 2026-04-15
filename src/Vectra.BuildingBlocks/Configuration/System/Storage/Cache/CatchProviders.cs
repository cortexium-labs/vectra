namespace Vectra.BuildingBlocks.Configuration.System.Storage.Cache;

public class CatchProviders
{
    public RedisCacheConfiguration Redis { get; set; } = new();
    public MemoryCacheConfiguration Memory { get; set; } = new();
}