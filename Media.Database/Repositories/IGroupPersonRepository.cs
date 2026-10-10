using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IGroupPersonRepository
{
    /// <summary>
    /// Upserts a group/person association: reactivates (and updates admin status on) any existing
    /// row for the pair, active or not, or inserts a new active row if none exists.
    /// </summary>
    /// <param name="audit">What the change is recorded as (DATABASE-63), in the same transaction.</param>
    Task<GroupPerson> UpsertAsync(int groupId, int personId, bool isAdmin, AuditEntry audit);

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

    /// <summary>The ids of the group's active admins (WORKER-29): who is told when someone leaves.</summary>
    Task<List<int>> ListActiveAdminIdsAsync(int groupId);

    /// <summary>The ids of the group's active members, admins included (DATABASE-75): who is told of a demoted device.</summary>
    Task<List<int>> ListActiveMemberIdsAsync(int groupId);

    /// <summary>
    /// Deactivates the active association for a (GroupId, PersonId) pair unless it is the group's
    /// last active admin -- the check and the change in one statement, so concurrent removals cannot
    /// both pass it. Returns the deactivated row, or <see langword="null"/> when nothing was active
    /// or the floor refused; read the membership again to tell which.
    /// </summary>
    /// <param name="audit">What the change is recorded as (DATABASE-63), in the same transaction; nothing is recorded when nothing changed.</param>
    Task<GroupPerson?> DeactivateKeepingAnAdminAsync(int groupId, int personId, AuditEntry audit);

    /// <summary>
    /// Makes an active admin a member only unless they are the group's last active admin, in one
    /// statement as <see cref="DeactivateKeepingAnAdminAsync"/>. Returns the updated row, or
    /// <see langword="null"/> when they were not an active admin or the floor refused.
    /// </summary>
    /// <param name="audit">What the change is recorded as (DATABASE-63), in the same transaction; nothing is recorded when nothing changed.</param>
    Task<GroupPerson?> DemoteKeepingAnAdminAsync(int groupId, int personId, AuditEntry audit);

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

    /// <summary>
    /// The admin's member list (API-178): a keyset page of a group's members, disabled ones
    /// included, each with <see cref="PersonIdentifier.IsMembershipActive"/>. Not for contacts or
    /// voice identification, which must see active members only.
    /// </summary>
    Task<List<PersonIdentifier>> GetMemberIdentifiersByGroupIdAsync(int groupId, PersonIdentifier? next, int limit);

    /// <summary>
    /// Resolves a <c>GroupPersonUuid</c> -- the multi-origin x-api-key model's third credential
    /// type (MEDIA-34) -- to the group it grants access to. Returns null unless the group/person
    /// association, the person, and the group are all active.
    /// </summary>
    Task<GroupAccess?> GetAccessByGroupPersonUuidAsync(Guid uuid);
}
