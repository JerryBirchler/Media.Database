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
        string? spokenName,
        bool isActive,
        int? createdByPersonId,
        bool isSuperAdmin,
        bool isEmailVerified,
        bool isSmsVerified,
        int? otpWindowOverrideMinutes,
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
            SpokenName = spokenName,
            IsActive = isActive,
            CreatedByPersonId = createdByPersonId,
            IsSuperAdmin = isSuperAdmin,
            IsEmailVerified = isEmailVerified,
            IsSmsVerified = isSmsVerified,
            OtpWindowOverrideMinutes = otpWindowOverrideMinutes,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
