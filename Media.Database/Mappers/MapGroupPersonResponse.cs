using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapGroupPersonResponse"/>
public class MapGroupPersonResponse : IMapGroupPersonResponse
{
    public GroupPerson ToGroupPerson(
        int groupPersonId,
        Guid groupPersonUuid,
        int groupId,
        int personId,
        bool isActive,
        bool isAdmin,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn)
    {
        return new GroupPerson
        {
            GroupPersonId = groupPersonId,
            GroupPersonUuid = groupPersonUuid,
            GroupId = groupId,
            PersonId = personId,
            IsActive = isActive,
            IsAdmin = isAdmin,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
