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

    /// <summary>
    /// Gets or sets the word-level changes derived from comparing the file's previous and new metadata.
    /// </summary>
    public List<ChangeWordRequest> Updates { get; set; } = [];
}
