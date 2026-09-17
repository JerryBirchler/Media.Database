using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes a <see cref="Group"/> from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked), matching
/// the same seam <see cref="IMapPersonResponse"/> already uses.
/// </summary>
public interface IMapGroupResponse
{
    /// <summary>
    /// Builds a <see cref="Group"/> from its already-extracted column values.
    /// </summary>
    Group ToGroup(
        int groupId,
        Guid groupUuid,
        string name,
        string title,
        string? description,
        bool isActive,
        bool isEncrypted,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn);
}
