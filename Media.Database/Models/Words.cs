using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a word record in the media database.
/// </summary>
public class Words
{
    /// <summary>
    /// Gets or sets the word's internal, sequential identifier. Never serialized -- exposing it
    /// would reveal insertion order/growth rate of the word index; <see cref="Uuid"/> is the
    /// externally-facing identifier.
    /// </summary>
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the word's externally-facing unique identifier.
    /// </summary>
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; set; }

    /// <summary>
    /// Gets or sets the word text.
    /// </summary>
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the word is a proper name.
    ///
    /// The same fact as <see cref="WordType"/> being <see cref="Models.WordType.ProperNoun"/>, kept
    /// as its own column because callers filter on it directly. Extraction sets both from one
    /// decision so they cannot disagree; it used to be re-derived by asking whether the first
    /// character was uppercase, which made every number a proper name.
    /// </summary>
    [JsonPropertyName("isProperName")]
    public bool IsProperName { get; set; } = false;

    /// <summary>
    /// Gets or sets the part of speech the word was tagged as when it was extracted.
    /// </summary>
    [JsonPropertyName("wordType")]
    public WordType WordType { get; set; } = WordType.None;

    /// <summary>
    /// Gets or sets the timestamp when the word was inserted.
    /// </summary>
    [JsonPropertyName("insertedOn")]
    public DateTimeOffset InsertedOn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the word was last updated.
    /// </summary>
    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// Gets or sets the ID of the file this word came from.
    /// </summary>
    [JsonPropertyName("cameFromFileId")]
    public Guid CameFromFileId { get; set; }
}
