namespace Media.Database.Providers;

/// <summary>
/// Provides PostgreSQL connection strings for database operations.
/// </summary>
public interface IPostgresConnectionProvider
{
    /// <summary>
    /// Gets the PostgreSQL connection string.
    /// </summary>
    /// <returns>The connection string for PostgreSQL database.</returns>
    string GetConnectionString();
}
