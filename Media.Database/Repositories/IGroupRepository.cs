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
}
