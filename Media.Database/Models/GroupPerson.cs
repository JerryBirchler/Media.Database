using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents one person's association with one group -- the row-level unit both group
/// membership and group-admin status (<see cref="IsAdmin"/>) live on. Peer-governed: any current
/// admin of the group can change any other member's <see cref="IsAdmin"/>, with no per-row
/// attribution of who granted it.
/// </summary>
public record GroupPerson
{
    /// <summary>
    /// Gets the integer identifier for this group/person association. Not <c>required</c>: see
    /// <see cref="Group.GroupId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int GroupPersonId { get; init; }

    /// <summary>
    /// Gets the unique identifier for this group/person association.
    /// </summary>
    public required Guid GroupPersonUuid { get; init; }

    /// <summary>
    /// Gets the identifier of the group this association belongs to. Not <c>required</c>; see
    /// <see cref="GroupPersonId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int GroupId { get; init; }

    /// <summary>
    /// Gets the identifier of the person this association belongs to. Not <c>required</c>; see
    /// <see cref="GroupPersonId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int PersonId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this association is active.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this person is an admin of the group. Peer-governed --
    /// see the type-level remarks.
    /// </summary>
    public required bool IsAdmin { get; init; }

    /// <summary>
    /// Gets the timestamp when this association was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this association was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
