using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IGroupRepository
{
    /// <summary>
    /// Creates a new group.
    /// </summary>
    Task<Group?> CreateAsync(string name, string title, string? description, bool isActive);

    /// <summary>
    /// Creates a group and, in the same transaction, makes <paramref name="ownerPersonId"/> its
    /// admin and attaches <paramref name="sourceMachineIds"/>.
    /// </summary>
    /// <remarks>
    /// One transaction because the three writes are one fact. Doing them separately produced a
    /// group with no members on 2026-09-25: the insert committed, the next call threw, and the
    /// row survived with nobody able to see or administer it -- every group read is scoped
    /// through GroupsPersons, so there is no route back to it from any UI.
    /// </remarks>
    Task<Group?> CreateOwnedAsync(
        string name,
        string title,
        string? description,
        bool isActive,
        int ownerPersonId,
        IReadOnlyList<int> sourceMachineIds);

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
    /// Sets a group's <see cref="Group.IsEncrypted"/> policy flag. Returns the updated group, or
    /// <see langword="null"/> if <paramref name="groupId"/> does not exist. Group-admin
    /// authorization is enforced by the caller, not here.
    /// </summary>
    Task<Group?> SetIsEncryptedAsync(int groupId, bool isEncrypted);

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
