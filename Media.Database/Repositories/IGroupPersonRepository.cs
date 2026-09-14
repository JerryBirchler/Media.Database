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
}
