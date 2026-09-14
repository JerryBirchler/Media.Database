using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IGroupPersonRepository
{
    /// <summary>
    /// Upserts a group/person association: reactivates (and updates admin status on) any existing
    /// row for the pair, active or not, or inserts a new active row if none exists.
    /// </summary>
    Task<GroupPerson> UpsertAsync(int groupId, int personId, bool isAdmin);

    /// <summary>
    /// Finds the active association for a (groupId, personId) pair, or <see langword="null"/> if
    /// none is active.
    /// </summary>
    Task<GroupPerson?> GetActiveAsync(int groupId, int personId);

    /// <summary>
    /// Deactivates the active association for a (groupId, personId) pair. Returns the updated
    /// association, or <see langword="null"/> if none was active.
    /// </summary>
    Task<GroupPerson?> DeactivateAsync(int groupId, int personId);

    /// <summary>
    /// Counts how many active admins <paramref name="groupId"/> currently has -- the "at least
    /// one admin must remain" floor check.
    /// </summary>
    Task<int> CountActiveAdminsAsync(int groupId);

    /// <summary>
    /// Identifies a keyset-paged page of the active groups <paramref name="personId"/> belongs to,
    /// ordered by name -- identifiers only (GroupId, Name), for cheap cursor computation before
    /// hydrating full rows via <see cref="IGroupRepository.GetByIdsAsync"/>. Pass
    /// <paramref name="next"/> null for the first page; its GroupId component is otherwise unused
    /// (a defensive tiebreaker only -- Name already carries a unique index).
    /// </summary>
    Task<List<(int GroupId, string Name)>> GetGroupIdentifiersByPersonIdAsync(int personId, (int GroupId, string Name)? next, int limit);

    /// <summary>
    /// Identifies a keyset-paged page of <paramref name="groupId"/>'s active members, ordered by
    /// last name then first name (ties broken by PersonUuid) -- identifiers only, for cheap cursor
    /// computation before hydrating full rows via <see cref="IPersonRepository.GetByIdsAsync"/>.
    /// Pass <paramref name="next"/> null for the first page; its PersonId is otherwise unused.
    /// </summary>
    Task<List<PersonIdentifier>> GetPersonIdentifiersByGroupIdAsync(int groupId, PersonIdentifier? next, int limit);
}
