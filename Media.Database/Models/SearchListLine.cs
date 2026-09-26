using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// One line of an OR list: what to look for, and optionally where to look and what kind of word to
/// accept. Lines within a list OR together; lists AND with one another.
///
/// Both filters are null for "any", and null is written as an absent property rather than a magic
/// value. The payload is versioned, and version 1 is what defines absent as any -- so the default
/// lives in one place instead of being stamped into every stored row.
/// </summary>
public record SearchListLine
{
    /// <summary>
    /// The text to match. Exact, always: for a name or a keyword that means the whole stored value,
    /// and for the From origins it means one extracted word.
    /// </summary>
    [JsonPropertyName("lookingFor")]
    public required string LookingFor { get; init; }

    /// <summary>
    /// Which metadata the word has to have come from, or null for any.
    /// </summary>
    [JsonPropertyName("metadataType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WordOrigin? MetadataType { get; init; }

    /// <summary>
    /// Which part of speech to accept, or null for any.
    ///
    /// Only meaningful for the From origins. A name is always <see cref="WordType.ProperNoun"/>
    /// and a keyword carries no type at all, so pairing this with either returns nothing.
    /// </summary>
    [JsonPropertyName("wordType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WordType? WordType { get; init; }
}
