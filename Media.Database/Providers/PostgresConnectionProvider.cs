using Media.Common.Helpers;

namespace Media.Database.Providers;

/// <summary>
/// Provides PostgreSQL connection strings from BaseStartup configuration.
/// </summary>
public class PostgresConnectionProvider : IPostgresConnectionProvider
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the PostgresConnectionProvider class.
    /// </summary>
    public PostgresConnectionProvider()
    {
        _connectionString = BaseStartup.PostgresConnectionString 
            ?? throw new InvalidOperationException("PostgreSQL connection string is not initialized in BaseStartup.");
    }

    /// <summary>
    /// Gets the PostgreSQL connection string.
    /// </summary>
    /// <returns>The connection string for PostgreSQL database.</returns>
    public string GetConnectionString() => _connectionString;
}
