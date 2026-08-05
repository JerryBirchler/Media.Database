using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record UpsertWordRequest : BaseWordRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override KafkaProducerActions Action { get; } = KafkaProducerActions.Upsert;
}