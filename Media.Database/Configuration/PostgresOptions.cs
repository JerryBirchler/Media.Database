namespace Media.Database.Configuration;

/// <summary>
/// Configuration options for PostgreSQL database connections.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    /// The configuration section name for binding these options.
    /// </summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string PostgresConnection { get; set; } = string.Empty;
}
