using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record BaseWordRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public virtual KafkaProducerActions Action { get; set; }
    public required string Word { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required WordOrigin Origin { get; set; }
    public required bool IsProperName { get; set; }
    public required Guid CameFromFileId { get; set; }
}