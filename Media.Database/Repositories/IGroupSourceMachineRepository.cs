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

    /// <summary>
    /// Identifies a keyset-paged page of <paramref name="groupId"/>'s active devices, ordered by
    /// device name -- identifiers only (SourceMachineId, SourceMachineName), for cheap cursor
    /// computation before hydrating full rows via <see cref="IRegistrationRepository.GetByIdsAsync"/>
    /// (the existing "registrations" Scylla table, no new table needed). Pass
    /// <paramref name="afterSourceMachineName"/>/<paramref name="afterSourceMachineId"/> null for
    /// the first page.
    /// </summary>
    Task<List<(int SourceMachineId, string SourceMachineName)>> GetSourceMachineIdentifiersByGroupIdAsync(int groupId, string? afterSourceMachineName, int? afterSourceMachineId, int limit);
}
