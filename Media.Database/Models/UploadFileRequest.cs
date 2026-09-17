using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Request model for uploading (creating or replacing) a file record.
/// </summary>
public record UploadFileRequest
{
    /// <summary>
    /// Gets or sets the original file path -- kept as the internal, always-stable identity across
    /// a file's versions. The wire-facing JSON name is simply "filePath".
    /// </summary>
    [JsonPropertyName("filePath")]
    [FromHeader(Name = "filePath")]
    public required string OriginalFilePath { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last file system update.
    /// </summary>
    [JsonPropertyName("lastFileUpdate")]
    [FromHeader(Name = "lastFileUpdate")]
    public DateTimeOffset? LastFileUpdate { get; set; }

    /// <summary>
    /// Gets or sets the file metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [FromHeader(Name = "metadata")]
    public Metadata? Metadata { get; set; }
}
