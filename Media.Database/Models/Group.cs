namespace Media.Database.Models;

/// <summary>
/// Represents a group -- the unit of collaborative access for the Groups/Persons admin API.
/// </summary>
public record Group
{
    /// <summary>
    /// Gets the integer identifier for the group.
    /// </summary>
    public required int GroupId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the group.
    /// </summary>
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
    /// Gets the timestamp when the group record was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when the group record was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
