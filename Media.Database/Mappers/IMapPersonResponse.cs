using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes a <see cref="Person"/> from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked), matching
/// the same seam <see cref="IMapRegistrationResponses"/> already uses.
/// </summary>
public interface IMapPersonResponse
{
    /// <summary>
    /// Builds a <see cref="Person"/> from its already-extracted column values.
    /// </summary>
    Person ToPerson(
        int personId,
        Guid personUuid,
        string emailAddress,
        string cellPhoneNumber,
        string firstName,
        string lastName,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset? updatedOn);
}
