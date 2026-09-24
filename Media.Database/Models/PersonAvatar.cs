namespace Media.Database.Models;

/// <summary>
/// A person's avatar picture (MEDIA-40): optional, shown in place of their initial. Small by
/// construction -- the browser shrinks it to 256x256 before upload -- and kept in Scylla only,
/// keyed by person, with no Postgres counterpart or CDC feed.
/// </summary>
public record PersonAvatar
{
    /// <summary>Gets the person the picture belongs to. Partition key.</summary>
    public required int PersonId { get; init; }

    /// <summary>Gets the picture's media type, for example <c>image/webp</c>.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the picture itself.</summary>
    public required byte[] Image { get; init; }

    /// <summary>Gets when the picture was last replaced; it doubles as the picture's version.</summary>
    public required DateTimeOffset UpdatedOn { get; init; }
}
