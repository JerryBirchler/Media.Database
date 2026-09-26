namespace Media.Database.Models;

/// <summary>
/// The Scylla half of a saved search list, as stored: the name and the payload still encrypted,
/// and the version that says how to read the payload once it is not.
///
/// Kept separate from <see cref="SearchList"/> because neither store holds a whole list. This is
/// what comes back from a partition read, before the application key has been anywhere near it.
/// </summary>
public record SearchListContent
{
    /// <summary>The borrowed Postgres identity, which is this row's clustering key.</summary>
    public required int Id { get; init; }

    /// <summary>The list's name, encrypted.</summary>
    public required string Name { get; init; }

    /// <summary>The lines, serialized and encrypted.</summary>
    public required string Payload { get; init; }

    /// <summary>
    /// The payload's shape. Lives here beside the blob rather than in Postgres, so a
    /// half-succeeded two-store write cannot leave the relational side asserting a shape this
    /// side does not have.
    /// </summary>
    public required int PayloadVersion { get; init; }
}
