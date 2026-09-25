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
    public virtual WordProducerActions Action { get; set; }

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
    /// Gets or sets the part of speech the word was tagged as.
    ///
    /// Deliberately not <c>required</c>, unlike its neighbours: adding it that way would break
    /// every existing construction site, and a producer that does not set it should mean
    /// "unclassified" rather than fail. <see cref="IsProperName"/> is the same fact as
    /// <see cref="Models.WordType.ProperNoun"/> and extraction sets both from one decision.
    ///
    /// <see cref="CreateWordRequest"/> deliberately does not carry this. That is the HTTP-facing
    /// shape, and whether a caller declares a word's type is an API contract question that has not
    /// been decided.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WordType WordType { get; set; } = WordType.None;

    /// <summary>
    /// Gets or sets the ID of the file this word came from.
    /// </summary>
    public required Guid CameFromFileId { get; set; }
}