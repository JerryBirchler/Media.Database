namespace Media.Database.Models;

/// <summary>
/// An active person found by last name and email address -- the add-a-member-by-details lookup
/// (MEDIA-8). Just enough to show an admin who matched and to add the one they pick; never the
/// cellphone number, which adding by details never uses.
/// </summary>
public record PersonMatch
{
    public int PersonId { get; init; }

    public Guid PersonUuid { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;
}
