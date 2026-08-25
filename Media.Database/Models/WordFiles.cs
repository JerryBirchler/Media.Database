using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Internal join-table record linking a word to the file it appears in.
/// </summary>
internal class WordFiles
{
    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the word.
    /// </summary>
    [JsonPropertyName("WordId")]
    public int WordId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the file.
    /// </summary>
    [JsonPropertyName("fileId")]
    public Guid FileId { get; set; }
}
