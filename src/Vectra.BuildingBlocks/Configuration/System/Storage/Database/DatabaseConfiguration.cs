namespace Vectra.BuildingBlocks.Configuration.System.Storage.Database;

public class DatabaseConfiguration
{
    public string DefaultProvider { get; set; } = "Sqlite";
    public DatabaseProviders Providers { get; set; } = new();
}