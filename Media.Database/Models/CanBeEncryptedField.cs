namespace Media.Database.Models;

/// <summary>
/// One [CanBeEncrypted]-decorated candidate -- the .NET type and member it was found on, which
/// stands in for a "table"/"column" identity in the CanBeEncryptedFields registry (MEDIA-12)
/// since object-level decoration (e.g. the whole <c>Metadata</c> class) doesn't correspond to a
/// single physical database column.
/// </summary>
public sealed record CanBeEncryptedField
{
    public required string TypeName { get; init; }

    public required string MemberName { get; init; }

    public int? ReleaseIntroduced { get; init; }

    public int? ReleaseRemoved { get; init; }
}
