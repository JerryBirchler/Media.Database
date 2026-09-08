namespace Media.Database.Models;

/// <summary>
/// Response model returned after updating a file record.
/// </summary>
public record UpdateFileResponse
{
    /// <summary>
    /// Gets or sets the updated file, or null if the file was not found.
    /// </summary>
    public Files? File { get; set; }
}
