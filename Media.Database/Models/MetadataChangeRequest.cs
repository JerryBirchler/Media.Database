using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record ChangeWordRequest
{
    public virtual KafkaProducerActions Action { get; set; }
    public required string PendingClause { get; set; }

    public string? CurrentClause { get; set; } = null;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required WordOrigin Origin { get; set; }
    public required Guid CameFromFileId { get; set; }
}