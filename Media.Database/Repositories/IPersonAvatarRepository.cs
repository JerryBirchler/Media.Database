using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Reads, replaces and removes a person's avatar picture (MEDIA-40). Scylla only.
/// </summary>
public interface IPersonAvatarRepository
{
    /// <summary>Gets <paramref name="personId"/>'s picture, or null when they have none.</summary>
    Task<PersonAvatar?> GetAsync(int personId);

    /// <summary>Writes (or replaces) a person's picture.</summary>
    Task SaveAsync(PersonAvatar avatar);

    /// <summary>Removes a person's picture; removing one that is not there is not an error.</summary>
    Task DeleteAsync(int personId);
}
