namespace Vectra.Infrastructure.Configuration.System.Storage.Database;

public class DatabaseConnection
{
    public string Provider { get; }
    public string ConnectionString { get; }

    public DatabaseConnection(string provider, string connectionString)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }
}