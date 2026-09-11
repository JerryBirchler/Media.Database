namespace Media.Database.Models;

/// <summary>
/// Represents a person, derived from a device's verified registration contact information.
/// </summary>
public record Person
{
    /// <summary>
    /// Gets the integer identifier for the person.
    /// </summary>
    public required int PersonId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the person.
    /// </summary>
    public required Guid PersonUuid { get; init; }

    /// <summary>
    /// Gets the person's email address.
    /// </summary>
    public required string EmailAddress { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's cell phone number.
    /// </summary>
    public required string CellPhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's first name.
    /// </summary>
    public required string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's last name.
    /// </summary>
    public required string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the person is active.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the timestamp when the person record was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when the person record was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
