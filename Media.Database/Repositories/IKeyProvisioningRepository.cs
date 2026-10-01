using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// The Postgres reads and the one write a device's encryption key provisioning needs (WORKER-16):
/// which devices are owed a key, and binding a shell exactly once. Kept apart from
/// <see cref="IRegistrationRepository"/> so Media.Worker, which provisions, needs only a SQL
/// executor -- not the registration repository's Scylla hydration and its dependencies.
/// </summary>
public interface IKeyProvisioningRepository
{
    /// <summary>
    /// The device, if it is active, verified on both channels and still without a group shell --
    /// the state in which its encryption key is owed; otherwise <see langword="null"/>.
    /// </summary>
    Task<ProvisioningCandidate?> GetCandidateAsync(int sourceMachineId);

    /// <summary>Up to <paramref name="limit"/> devices owed an encryption key, oldest first.</summary>
    Task<List<ProvisioningCandidate>> GetCandidatesAsync(int limit);

    /// <summary>
    /// Permanently sets <c>SourceMachineRegistrations.GroupShellId</c> to <paramref name="groupShellId"/>
    /// for <paramref name="sourceMachineId"/> -- a no-op if already set (MEDIA-37: a device's shell
    /// assignment is a one-time thing this can make, never a reassignment).
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when this call bound the shell; <see langword="false"/> when the device
    /// already had one.
    /// </returns>
    Task<bool> BindGroupShellIfUnboundAsync(int sourceMachineId, int groupShellId);
}
