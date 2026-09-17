using Media.Common.Serialization;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a group -- the unit of collaborative access for the Groups/Persons admin API.
/// </summary>
public record Group
{
    /// <summary>
    /// Gets the integer identifier for the group. Not <c>required</c>: System.Text.Json refuses to
    /// build type metadata for a <c>required</c> member that is also <see cref="JsonIgnoreAttribute"/>-
    /// decorated, since JSON could never satisfy it -- every construction site still sets this via
    /// object initializer instead.
    /// </summary>
    [JsonIgnore]
    public int GroupId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the group. Omitted from JSON entirely when redacted (zeroed
    /// to <see cref="Guid.Empty"/>, which is also <c>Guid</c>'s CLR default) -- not just replaced
    /// with a visible sentinel value.
    /// </summary>
    [Redactable]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required Guid GroupUuid { get; init; }

    /// <summary>
    /// Gets the group's unique, case-insensitive name.
    /// </summary>
    public required string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the group's title. Required, but carries no uniqueness constraint.
    /// </summary>
    public required string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the group's description, or <see langword="null"/>.
    /// </summary>
    public required string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the group is active.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether encryption at rest is enabled for this group's
    /// CanBeEncrypted data -- group-wide policy, settable only by a group admin. A device's own
    /// <see cref="SourceMachineRegistrations.IsEncrypted"/>, when explicitly set, overrides this
    /// (see the COALESCE resolution in MEDIA-11).
    /// </summary>
    public required bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets the timestamp when the group record was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when the group record was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
