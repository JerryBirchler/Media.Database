using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IPersonRepository
{
    /// <summary>
    /// Finds the person matching <paramref name="firstName"/>, <paramref name="lastName"/>,
    /// <paramref name="emailAddress"/>, and <paramref name="cellPhoneNumber"/> (the same tuple
    /// <c>IX_Persons_ContactInformation</c> enforces uniqueness on), or creates one if none exists.
    /// </summary>
    Task<Person?> FindOrCreateAsync(string firstName, string lastName, string emailAddress, string cellPhoneNumber);

    /// <summary>
    /// Finds the person matching <paramref name="personUuid"/>, or <see langword="null"/> if none
    /// exists. Used by <c>PersonApiKeyAuthenticationHandler</c> to resolve the X-API-KEY header
    /// for the Groups/Persons admin API.
    /// </summary>
    Task<Person?> GetByUuidAsync(Guid personUuid);

    /// <summary>
    /// Creates a new person with an explicit creator -- the <c>POST /api/persons</c> path.
    /// </summary>
    Task<Person?> CreateAsync(string firstName, string lastName, string emailAddress, string cellPhoneNumber, int createdByPersonId);

    /// <summary>
    /// Gets a keyset-paginated page of PersonId/PersonUuid/LastName/FirstName identifiers for
    /// <paramref name="createdByPersonId"/> themselves plus every person they created (active and
    /// inactive alike), ordered by last name then first name (ties broken by PersonUuid) -- the
    /// non-super-admin view of <c>GET /api/persons/{next}/pages</c>.
    /// </summary>
    Task<List<PersonIdentifier>> GetPersonIdentifiersByCreatorIdAsync(int createdByPersonId, PersonIdentifier? next, int limit);

    /// <summary>
    /// Same shape and ordering as <see cref="GetPersonIdentifiersByCreatorIdAsync"/>, minus the
    /// creator scope -- the super-admin view of <c>GET /api/persons/{next}/pages</c>.
    /// </summary>
    Task<List<PersonIdentifier>> GetAllPersonIdentifiersAsync(PersonIdentifier? next, int limit);

    /// <summary>
    /// Sets a person's <see cref="Person.IsActive"/> flag. Returns the updated person, or
    /// <see langword="null"/> if <paramref name="personId"/> does not exist.
    /// </summary>
    Task<Person?> SetActiveAsync(int personId, bool isActive);

    /// <summary>
    /// Updates a person's contact fields and active/verification flags -- the self-update path
    /// (<c>PUT /api/persons</c>). The caller computes <paramref name="isActive"/>,
    /// <paramref name="isEmailVerified"/>, and <paramref name="isSmsVerified"/> (e.g. resetting
    /// verification when email/cell phone number changed); this just persists them atomically
    /// alongside the contact fields. Returns the updated person, or <see langword="null"/> if
    /// <paramref name="personId"/> does not exist.
    /// </summary>
    Task<Person?> UpdateAsync(int personId, string firstName, string lastName, string emailAddress, string cellPhoneNumber, bool isActive, bool isEmailVerified, bool isSmsVerified);

    /// <summary>
    /// Raises <paramref name="personId"/>'s IsEmailVerified/IsSmsVerified flags to true where the
    /// incoming value is true, never lowering an already-true flag back to false. Returns the
    /// updated person, or <see langword="null"/> if <paramref name="personId"/> does not exist.
    /// </summary>
    Task<Person?> SetVerifiedIfTrueAsync(int personId, bool isEmailVerified, bool isSmsVerified);

    /// <summary>
    /// Hydrates full <see cref="Person"/> rows for a set of ids -- Postgres identifies which
    /// persons and in what order (e.g. the "group persons" list), this hydrates the full row
    /// content preferring Scylla, falling back to PostgreSQL per row when Scylla doesn't have it
    /// yet (CDC lag) or is unreachable.
    /// </summary>
    /// <param name="personIds">The person ids to hydrate.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent lookups.</param>
    /// <returns>The hydrated persons, in no particular order -- callers that need a specific order must reorder by id themselves.</returns>
    Task<List<Person>> GetByIdsAsync(IEnumerable<int> personIds, int maxDegreeOfParallelism);
}
