using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IPersonSourceMachineRepository
{
    /// <summary>
    /// Finds the active association for a (personId, sourceMachineId) pair, or
    /// <see langword="null"/> if none is active.
    /// </summary>
    Task<PersonSourceMachine?> GetActiveAsync(int personId, int sourceMachineId);

    /// <summary>
    /// Inserts a new active person/device association.
    /// </summary>
    Task<PersonSourceMachine?> CreateAsync(int personId, int sourceMachineId);

    /// <summary>
    /// Lists every active device association for a person -- used when creating a group to
    /// auto-associate all of the creator's currently-active devices (MEDIA-8).
    /// </summary>
    Task<List<PersonSourceMachine>> ListActiveByPersonAsync(int personId);
}
