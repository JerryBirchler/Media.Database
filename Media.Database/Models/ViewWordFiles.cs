using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// A row from the word/file materialized view, pairing a word with the file it was found in.
/// </summary>
public class ViewWordFiles
{
    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the word.
    /// </summary>
    [JsonPropertyName("wordId")]
    public int WordId { get; set; }

    /// <summary>
    /// Gets or sets the word text.
    /// </summary>
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the file the word was found in.
    /// </summary>
    [JsonPropertyName("fileId")]
    public Guid FileId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the associated file is the current version.
    /// </summary>
    [JsonPropertyName("isCurrent")]
    public bool? IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the word is a proper name.
    /// </summary>
    [JsonPropertyName("isProperName")]
    public bool? IsProperName { get; set; }
}
