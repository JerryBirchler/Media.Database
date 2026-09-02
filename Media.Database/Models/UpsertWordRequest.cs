using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Request model for inserting or updating a word record.
/// </summary>
public record UpsertWordRequest : BaseWordRequest
{
    /// <summary>
    /// Gets or sets the Kafka producer action, defaulting to Upsert.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override WordProducerActions Action { get; set; } = WordProducerActions.Upsert;
}