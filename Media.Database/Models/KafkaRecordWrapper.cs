using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Wrapper record for a Kafka message containing a word request.
/// </summary>
/// <param name="Value">The word request payload.</param>
public record KafkaRecordWrapper
(
    [property: JsonPropertyName("value")]
    BaseWordRequest Value
);
