using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IGroupRepository
{
    /// <summary>
    /// Creates a new group.
    /// </summary>
    Task<Group?> CreateAsync(string name, string title, string? description, bool isActive);

    /// <summary>
    /// Finds the group matching <paramref name="groupUuid"/>, or <see langword="null"/> if none exists.
    /// </summary>
    Task<Group?> GetByUuidAsync(Guid groupUuid);

    /// <summary>
    /// Finds the group matching <paramref name="name"/> (case-insensitive), or <see langword="null"/> if none exists.
    /// </summary>
    Task<Group?> GetByNameAsync(string name);

    /// <summary>
    /// Partially updates a group's <paramref name="title"/>/<paramref name="description"/> -- a
    /// <see langword="null"/> argument leaves that field unchanged. Returns the updated group, or
    /// <see langword="null"/> if <paramref name="groupId"/> does not exist.
    /// </summary>
    Task<Group?> UpdateAsync(int groupId, string? title, string? description);

    /// <summary>
    /// Sets a group's <see cref="Group.IsActive"/> flag. Returns the updated group, or
    /// <see langword="null"/> if <paramref name="groupId"/> does not exist.
    /// </summary>
    Task<Group?> SetActiveAsync(int groupId, bool isActive);

    /// <summary>
    /// Hydrates full <see cref="Group"/> rows for a set of ids -- Postgres identifies which groups
    /// and in what order (e.g. the "groups a person belongs to" list), this hydrates the full row
    /// content preferring Scylla, falling back to PostgreSQL per row when Scylla doesn't have it
    /// yet (CDC lag) or is unreachable.
    /// </summary>
    /// <param name="groupIds">The group ids to hydrate.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent lookups.</param>
    /// <returns>The hydrated groups, in no particular order -- callers that need a specific order must reorder by id themselves.</returns>
    Task<List<Group>> GetByIdsAsync(IEnumerable<int> groupIds, int maxDegreeOfParallelism);
}
