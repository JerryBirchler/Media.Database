using System.Text.Json.Serialization;

namespace Media.Database.Models
{
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
}
