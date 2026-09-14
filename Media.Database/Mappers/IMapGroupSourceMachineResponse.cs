using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes a <see cref="GroupSourceMachine"/> from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked), matching
/// the same seam <see cref="IMapPersonResponse"/> already uses.
/// </summary>
public interface IMapGroupSourceMachineResponse
{
    /// <summary>
    /// Builds a <see cref="GroupSourceMachine"/> from its already-extracted column values.
    /// </summary>
    GroupSourceMachine ToGroupSourceMachine(
        int groupSourceMachineId,
        Guid groupSourceMachineUuid,
        int groupId,
        int sourceMachineId,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn);
}
