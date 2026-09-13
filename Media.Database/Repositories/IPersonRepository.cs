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
}
