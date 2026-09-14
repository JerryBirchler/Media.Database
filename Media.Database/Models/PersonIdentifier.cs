namespace Media.Database.Models;

/// <summary>
/// The ordering-relevant identity of one person within a group's person list -- just enough to
/// compute a keyset pagination cursor and to hydrate the full row later (see
/// <see cref="Repositories.IPersonRepository.GetByIdsAsync"/>), without paying for a full
/// <see cref="Person"/> row when it isn't going to be rendered (e.g. a look-ahead page's own
/// identifiers, only ever used for their cursor value).
/// </summary>
public class PersonIdentifier
{
    /// <summary>
    /// Gets or sets the person's internal, sequential identifier -- the hydration key into
    /// <see cref="Repositories.IPersonRepository.GetByIdsAsync"/>, never exposed externally.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>Gets or sets the person's externally-facing unique identifier -- the final cursor tiebreaker.</summary>
    public Guid PersonUuid { get; set; }

    /// <summary>Gets or sets the person's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the person's first name.</summary>
    public string FirstName { get; set; } = string.Empty;
}
