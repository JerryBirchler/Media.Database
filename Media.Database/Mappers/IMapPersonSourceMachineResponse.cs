using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes a <see cref="PersonSourceMachine"/> from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked), matching
/// the same seam <see cref="IMapPersonResponse"/> already uses.
/// </summary>
public interface IMapPersonSourceMachineResponse
{
    /// <summary>
    /// Builds a <see cref="PersonSourceMachine"/> from its already-extracted column values.
    /// </summary>
    PersonSourceMachine ToPersonSourceMachine(
        int personSourceMachineId,
        Guid personSourceMachineUuid,
        int personId,
        int sourceMachineId,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn);
}
