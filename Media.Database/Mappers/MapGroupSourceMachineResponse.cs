using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapGroupSourceMachineResponse"/>
public class MapGroupSourceMachineResponse : IMapGroupSourceMachineResponse
{
    public GroupSourceMachine ToGroupSourceMachine(
        int groupSourceMachineId,
        Guid groupSourceMachineUuid,
        int groupId,
        int sourceMachineId,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn)
    {
        return new GroupSourceMachine
        {
            GroupSourceMachineId = groupSourceMachineId,
            GroupSourceMachineUuid = groupSourceMachineUuid,
            GroupId = groupId,
            SourceMachineId = sourceMachineId,
            IsActive = isActive,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
