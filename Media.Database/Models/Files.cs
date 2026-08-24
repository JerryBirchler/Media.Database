using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a file record in the media database.
/// </summary>
public class Files
{
    /// <summary>
    /// Gets or sets a value indicating whether this file exists in the database.
    /// </summary>
    [JsonIgnore]
    public bool Exists { get; internal set; } = false;

    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the source machine identifier.
    /// </summary>
    [JsonPropertyName("sourceMachineId")]
    public int SourceMachineId { get; set; }

    /// <summary>
    /// Gets or sets the original file path.
    /// </summary>
    [JsonPropertyName("originFilePath")]
    public string OriginalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the file was inserted.
    /// </summary>
    [JsonPropertyName("insertOn")]
    public DateTimeOffset InsertedOn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the file was last updated.
    /// </summary>
    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last file system update.
    /// </summary>
    [JsonPropertyName("lastFileUpdate")]
    public DateTimeOffset? LastFileUpdate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current version of the file.
    /// </summary>
    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets the file metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }
}
