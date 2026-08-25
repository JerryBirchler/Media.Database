using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a single word-level change (add, update, or delete) derived from comparing
/// a file's previous and new metadata, destined for publication to Kafka.
/// </summary>
public record ChangeWordRequest
{
    /// <summary>
    /// Gets or sets the Kafka producer action to perform for this word.
    /// </summary>
    public virtual KafkaProducerActions Action { get; set; }

    /// <summary>
    /// Gets or sets the new word text.
    /// </summary>
    public required string NewSpan { get; set; }

    /// <summary>
    /// Gets or sets the previous word text, when this change replaces an existing word.
    /// </summary>
    public string? CurrentSpan { get; set; } = null;

    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets the ID of the file this word came from.
    /// </summary>
    public required Guid CameFromFileId { get; set; }
}