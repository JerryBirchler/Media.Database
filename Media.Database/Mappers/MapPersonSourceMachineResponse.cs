using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapPersonSourceMachineResponse"/>
public class MapPersonSourceMachineResponse : IMapPersonSourceMachineResponse
{
    public PersonSourceMachine ToPersonSourceMachine(
        int personSourceMachineId,
        Guid personSourceMachineUuid,
        int personId,
        int sourceMachineId,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn)
    {
        return new PersonSourceMachine
        {
            PersonSourceMachineId = personSourceMachineId,
            PersonSourceMachineUuid = personSourceMachineUuid,
            PersonId = personId,
            SourceMachineId = sourceMachineId,
            IsActive = isActive,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };
    }
}
