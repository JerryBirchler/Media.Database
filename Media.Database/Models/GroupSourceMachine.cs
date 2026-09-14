namespace Media.Database.Models;

/// <summary>
/// Represents one group's association with one device -- how group-admin authority (see
/// <see cref="GroupPerson.IsAdmin"/>) reaches a device, per MEDIA-8: adding/removing a device from
/// a group requires being an admin of that group, no separate device-level admin concept involved.
/// </summary>
public record GroupSourceMachine
{
    /// <summary>
    /// Gets the integer identifier for this group/device association.
    /// </summary>
    public required int GroupSourceMachineId { get; init; }

    /// <summary>
    /// Gets the unique identifier for this group/device association.
    /// </summary>
    public required Guid GroupSourceMachineUuid { get; init; }

    /// <summary>
    /// Gets the identifier of the group this association belongs to.
    /// </summary>
    public required int GroupId { get; init; }

    /// <summary>
    /// Gets the identifier of the device this association belongs to.
    /// </summary>
    public required int SourceMachineId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this association is active.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the timestamp when this association was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this association was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
