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
    /// Finds the single active group/device association row for <paramref name="sourceMachineId"/>,
    /// or <see langword="null"/> if none. MEDIA-11: a device may belong to at most one active group.
    /// </summary>
    Task<GroupSourceMachine?> GetActiveBySourceMachineIdAsync(int sourceMachineId);

    /// <summary>
    /// Deactivates the active association for a (groupId, sourceMachineId) pair. Returns the
    /// updated association, or <see langword="null"/> if none was active.
    /// </summary>
    Task<GroupSourceMachine?> DeactivateAsync(int groupId, int sourceMachineId);

    /// <summary>
    /// Identifies a keyset-paged page of <paramref name="groupId"/>'s devices, ordered by device
    /// name -- identifiers only (SourceMachineId, SourceMachineName), for cheap cursor computation
    /// before hydrating full rows via <see cref="IRegistrationRepository.GetByIdsAsync"/> (the
    /// existing "registrations" Scylla table, no new table needed). The group/device association
    /// itself is always required to be active; <paramref name="includeInactive"/> controls only
    /// whether an inactive device registration is included (MEDIA-8: only a group admin may pass
    /// true). Pass <paramref name="next"/> null for the first page.
    /// </summary>
    Task<List<(int SourceMachineId, string SourceMachineName)>> GetSourceMachineIdentifiersByGroupIdAsync(int groupId, bool includeInactive, (int SourceMachineId, string SourceMachineName)? next, int limit);

    /// <summary>
    /// Resolves a group-scoped caller's deviceName/deviceType/disambiguationKey triple (MEDIA-34)
    /// to the SourceMachineId of one specific active device within <paramref name="groupId"/>, or
    /// <see langword="null"/> if no active device in that group matches. See
    /// <see cref="Queries.QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql"/>.
    /// </summary>
    Task<int?> GetActiveSourceMachineIdByGroupAndDisambiguationAsync(int groupId, string sourceMachineName, DeviceTypes deviceType, string disambiguationKey);
}
