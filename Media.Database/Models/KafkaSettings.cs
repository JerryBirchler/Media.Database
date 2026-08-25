namespace Media.Database.Models;

/// <summary>
/// Connection settings for the Kafka cluster used to publish word change events.
/// </summary>
public record KafkaSettings
{
    /// <summary>
    /// Gets or sets the base URL of the Kafka REST endpoint.
    /// </summary>
    public required Uri BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the port number for the Kafka connection.
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the Kafka cluster.
    /// </summary>
    public required string ClusterId { get; set; }
}