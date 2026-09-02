using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Request model for deleting a word record.
/// </summary>
public record DeleteWordRequest : BaseWordRequest
{
    /// <summary>
    /// Gets or sets the Kafka producer action, defaulting to Delete.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override WordProducerActions Action { get; set; } = WordProducerActions.Delete;
}