namespace Media.Database.Models;

/// <summary>
/// Represents one person's association with one device. Always a plain, passive association --
/// carries no admin or ownership concept of its own. Device ownership lives on
/// <c>SourceMachineRegistrations.OwningPersonId</c> instead (see MEDIA-10), not here.
/// </summary>
public record PersonSourceMachine
{
    /// <summary>
    /// Gets the integer identifier for this person/device association.
    /// </summary>
    public required int PersonSourceMachineId { get; init; }

    /// <summary>
    /// Gets the unique identifier for this person/device association.
    /// </summary>
    public required Guid PersonSourceMachineUuid { get; init; }

    /// <summary>
    /// Gets the identifier of the person this association belongs to.
    /// </summary>
    public required int PersonId { get; init; }

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
