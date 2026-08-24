namespace Media.Database.Models;

/// <summary>
/// Request model for creating a new word record.
/// </summary>
public record CreateWordRequest
{
    /// <summary>
    /// Gets or sets the word text.
    /// </summary>
    public required string Word { get; set; }

    /// <summary>
    /// Gets or sets the origin of the word.
    /// </summary>
    public required WordOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the word is a proper name.
    /// </summary>
    public required bool IsProperName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the file this word came from.
    /// </summary>
    public required Guid CameFromFileId { get; set; }
}
