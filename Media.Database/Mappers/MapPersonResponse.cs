using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapPersonResponse"/>
public class MapPersonResponse : IMapPersonResponse
{
    public Person ToPerson(
        int personId,
        Guid personUuid,
        string emailAddress,
        string cellPhoneNumber,
        string firstName,
        string lastName,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn)
    {
        return new Person
        {
            PersonId = personId,
            PersonUuid = personUuid,
            EmailAddress = emailAddress,
            CellPhoneNumber = cellPhoneNumber,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
