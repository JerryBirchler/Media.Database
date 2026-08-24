using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Base record for word-related requests.
/// </summary>
public record BaseWordRequest
{
    /// <summary>
    /// Gets or sets the Kafka producer action to perform.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public virtual KafkaProducerActions Action { get; set; }

    /// <summary>
    /// Gets or sets the word text.
    /// </summary>
    public required string Word { get; set; }

    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the word is a proper name.
    /// </summary>
    public required bool IsProperName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the file this word came from.
    /// </summary>
    public required Guid CameFromFileId { get; set; }
}