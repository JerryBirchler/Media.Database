using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapGroupResponse"/>
public class MapGroupResponse : IMapGroupResponse
{
    public Group ToGroup(
        int groupId,
        Guid groupUuid,
        string name,
        string title,
        string? description,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn)
    {
        return new Group
        {
            GroupId = groupId,
            GroupUuid = groupUuid,
            Name = name,
            Title = title,
            Description = description,
            IsActive = isActive,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
