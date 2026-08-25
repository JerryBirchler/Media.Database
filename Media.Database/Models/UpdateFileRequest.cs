namespace Media.Database.Models;

/// <summary>
/// Request model for updating an existing file record.
/// </summary>
public record UpdateFileRequest
{
    /// <summary>
    /// Gets or sets the timestamp of the last file system update.
    /// </summary>
    public DateTimeOffset? LastFileUpdate { get; set; }

    /// <summary>
    /// Gets or sets the updated file metadata.
    /// </summary>
    public Metadata? Metadata { get; set; }
}
