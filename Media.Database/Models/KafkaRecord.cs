using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a Kafka record containing word creation information.
/// </summary>
/// <param name="Topic">The Kafka topic name.</param>
/// <param name="Partition">The partition number.</param>
/// <param name="Offset">The message offset.</param>
/// <param name="Value">The word creation request payload.</param>
public record KafkaRecord(
    [property: JsonPropertyName("topic")]
    string Topic,

    [property: JsonPropertyName("partition")]
    int Partition,

    [property: JsonPropertyName("offset")]
    long Offset,

    [property: JsonPropertyName("value")]
    CreateWordRequest Value
);
