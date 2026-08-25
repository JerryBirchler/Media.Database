using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a word record in the media database.
/// </summary>
public class Words
{
    /// <summary>
    /// Gets or sets the unique identifier for the word.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

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
    /// </summary>
    [JsonPropertyName("isProperName")]
    public bool IsProperName { get; set; } = false;

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
