namespace Vectra.BuildingBlocks.Configuration.System.Storage.Cache;

public class CacheConfiguration
{
    public string DefaultProvider { get; set; } = "Redis";
    public CatchProviders Providers { get; set; } = new();
}