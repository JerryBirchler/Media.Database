using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// A lightweight identity anchor auto-created the moment a device registers with no existing
/// group (MEDIA-37) -- exists so encryption keys (see <see cref="GroupEncryptionKey"/>) and the
/// UUID orchestration table always have something to reference, without requiring Groups' Name/
/// Title to be populated before real investment in a named group exists. Promotion to a real,
/// named group (not yet built) sets <see cref="PromotedGroupId"/>.
/// </summary>
public record GroupShell
{
    /// <summary>
    /// Gets the integer identifier for this shell. Not <c>required</c>; see
    /// <see cref="Group.GroupId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int GroupShellId { get; init; }

    /// <summary>
    /// Gets the identifier of the real group this shell was promoted to, or null if it has not
    /// been promoted.
    /// </summary>
    public int? PromotedGroupId { get; init; }

    /// <summary>
    /// Gets the timestamp when this shell was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this shell was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
