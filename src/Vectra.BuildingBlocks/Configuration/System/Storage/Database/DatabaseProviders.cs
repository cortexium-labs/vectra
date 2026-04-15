namespace Vectra.BuildingBlocks.Configuration.System.Storage.Database;

public class DatabaseProviders
{
    public SqliteConfiguration Sqlite { get; set; } = new();
    public PostgreSqlConfiguration Postgres { get; set; } = new();
}