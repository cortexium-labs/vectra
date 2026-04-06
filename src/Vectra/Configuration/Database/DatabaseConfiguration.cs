using Vectra.Infrastructure.Persistence.Common;

namespace Vectra.Configuration.Database;

public class DatabaseConfiguration
{
    public string Default { get; set; } = "SQLite";
    public Dictionary<string, DatabaseConnection> Connections { get; set; } = new();

    public DatabaseConnection GetActiveConnection()
    {
        if (Connections.TryGetValue(Default, out var connection))
            return connection;

        var fallback = "Data Source=vectra.db";
        return new DatabaseConnection("SQLite", fallback);
    }
}