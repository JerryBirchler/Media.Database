using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IGroupSourceMachineRepository
{
    /// <summary>
    /// Upserts a group/device association: reactivates any existing row for the pair, active or
    /// not, or inserts a new active row if none exists.
    /// </summary>
    Task<GroupSourceMachine> UpsertAsync(int groupId, int sourceMachineId);

    /// <summary>
    /// Deactivates the active association for a (groupId, sourceMachineId) pair. Returns the
    /// updated association, or <see langword="null"/> if none was active.
    /// </summary>
    Task<GroupSourceMachine?> DeactivateAsync(int groupId, int sourceMachineId);
}
