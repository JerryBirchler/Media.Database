using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record DeleteWordRequest : BaseWordRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override KafkaProducerActions Action { get; } = KafkaProducerActions.Delete;
}