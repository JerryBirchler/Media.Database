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
    /// Gets or sets the original path of the file the word was found in.
    /// </summary>
    [JsonPropertyName("originalFilePath")]
    public string OriginalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the word is a proper name.
    /// </summary>
    [JsonPropertyName("isProperName")]
    public bool? IsProperName { get; set; }

    /// <summary>
    /// Gets or sets the device that owns the file this word was found in. Server-side only --
    /// used to validate that a fileId/filePath-scoped lookup actually belongs to the requesting
    /// device before returning anything about it. Never serialized: raw internal device
    /// identifiers were deliberately stripped from API responses elsewhere in this codebase for
    /// the same reason.
    /// </summary>
    [JsonIgnore]
    public int SourceMachineId { get; set; }
}
