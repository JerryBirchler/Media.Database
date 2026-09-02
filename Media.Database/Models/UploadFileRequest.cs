using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Request model for uploading (creating or replacing) a file record.
/// </summary>
public record UploadFileRequest
{
    /// <summary>
    /// Gets or sets the original file path.
    /// </summary>
    [JsonPropertyName("originalFilePath")]
    [FromHeader(Name = "originalFilePath")]
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
