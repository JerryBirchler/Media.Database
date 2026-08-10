using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KafkaProducerActions
    {
        Add,
        Upsert,
        Update,
        Delete
    }
}
