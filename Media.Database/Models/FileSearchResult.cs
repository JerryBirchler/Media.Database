namespace Media.Database.Models;

/// <summary>
/// One file a search matched.
///
/// Identification only -- the file's own detail is hydrated from Scylla afterwards, as everywhere
/// else. The search's job is deciding which files qualify, and it does that entirely inside
/// Postgres against the word index.
/// </summary>
public record FileSearchResult
{
    /// <summary>The file.</summary>
    public required Guid FileId { get; init; }

    /// <summary>Its path, which is also the order results come back in.</summary>
    public required string OriginalFilePath { get; init; }

    /// <summary>Whether it still exists where it came from.</summary>
    public required bool IsCurrent { get; init; }

    /// <summary>The device it came from.</summary>
    public required int SourceMachineId { get; init; }
}
