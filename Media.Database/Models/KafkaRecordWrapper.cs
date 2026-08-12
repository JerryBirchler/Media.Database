using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record KafkaRecordWrapper
(
    [property: JsonPropertyName("value")]
    BaseWordRequest Value
);
