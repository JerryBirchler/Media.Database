using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes a <see cref="GroupPerson"/> from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked), matching
/// the same seam <see cref="IMapPersonResponse"/> already uses.
/// </summary>
public interface IMapGroupPersonResponse
{
    /// <summary>
    /// Builds a <see cref="GroupPerson"/> from its already-extracted column values.
    /// </summary>
    GroupPerson ToGroupPerson(
        int groupPersonId,
        Guid groupPersonUuid,
        int groupId,
        int personId,
        bool isActive,
        bool isAdmin,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn);
}
