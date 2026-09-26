using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// A saved search list, assembled from both stores.
///
/// Postgres holds the skeleton -- identity, owner, type, timestamps -- and Scylla holds the name
/// and the lines, both encrypted. Neither store holds a whole list on its own, which is the point:
/// the relational side keeps a real foreign key with a real delete rule, and no user content ever
/// lands in it.
///
/// Both shapes share the row and the payload column; <see cref="SearchListType"/> says which is
/// populated. An OR list carries lines, an AND list carries the uuids of the lists it combines.
/// </summary>
public class SearchList
{
    /// <summary>
    /// The identity Postgres issues. Scylla borrows it as its clustering key rather than
    /// carrying an identity of its own.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The public identity. Routes address it, and an AND list references it -- so it is a value
    /// a client has to hold, not just a URL it follows.
    /// </summary>
    public Guid Uuid { get; set; }

    /// <summary>Which pair of tables this came from.</summary>
    public OwnerScope Scope { get; set; }

    /// <summary>The owning person or group, per <see cref="Scope"/>.</summary>
    public int OwnerId { get; set; }

    /// <summary>Whether the payload holds lines or references.</summary>
    public SearchListType ListType { get; set; } = SearchListType.None;

    /// <summary>
    /// The list's display name, decrypted. Encrypted at rest for the same reason the lines are:
    /// no classifier distinguishes "the girls" from "medical records", and encrypting only what
    /// looks sensitive makes the choice itself a disclosure.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The shape of the stored payload. Lives beside the blob in Scylla rather than in Postgres,
    /// so a half-succeeded two-store write cannot leave the relational side asserting a shape the
    /// blob does not have.
    /// </summary>
    public int PayloadVersion { get; set; } = CurrentPayloadVersion;

    /// <summary>The lines, in the order the user arranged them. OR within a list.</summary>
    public IReadOnlyList<SearchListLine> Lines { get; set; } = [];

    /// <summary>
    /// For an AND list, the OR lists it combines, by uuid. Resolved under the caller's own scope
    /// every time it is used rather than trusted from when it was saved -- ownership can change
    /// underneath a reference, and a search that runs somebody else's list is a decryption
    /// oracle.
    /// </summary>
    public IReadOnlyList<Guid> References { get; set; } = [];

    /// <summary>When the list was created.</summary>
    public DateTimeOffset InsertedOn { get; set; }

    /// <summary>When it was last edited, or null if never.</summary>
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// The payload shape this build writes. Version 1: an object with a "lines" array, where an
    /// absent metadata type or word type means any.
    /// </summary>
    [JsonIgnore]
    public const int CurrentPayloadVersion = 1;
}
