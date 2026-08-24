namespace Media.Database.Configuration;

/// <summary>
/// Configuration options for ScyllaDB NoSQL database connections.
/// </summary>
public class ScyllaOptions
{
    /// <summary>
    /// The configuration section name for binding these options.
    /// </summary>
    public const string SectionName = "ScyllaDB";

    /// <summary>
    /// Gets or sets the list of internal contact points for the ScyllaDB cluster.
    /// </summary>
    public List<string> ContactPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of external contact points for the ScyllaDB cluster.
    /// </summary>
    public List<string> ExternalContactPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the port number for ScyllaDB connections.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the keyspace name to use for ScyllaDB operations.
    /// </summary>
    public string Keyspace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum batch size for ScyllaDB batch operations.
    /// </summary>
    public int MaxBatchsize { get; set; } = 100;
}
