using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides enrollment, lookup and revocation for the public keys a device authenticates with.
/// Only public halves are ever stored -- the private key never leaves the device, so a dump of this
/// table lets nobody authenticate as anything.
/// </summary>
public interface ISourceMachineKeyRepository
{
    /// <summary>
    /// Enrolls a public key for a device.
    /// </summary>
    /// <exception cref="Npgsql.PostgresException">
    /// <paramref name="publicKey"/> has been enrolled before -- by this device or any other, active
    /// or revoked. Re-enrolling a key is refused on purpose: it is what would otherwise let an
    /// attacker undo a revocation by enrolling the key they stole.
    /// </exception>
    Task<SourceMachineKey> EnrollAsync(int sourceMachineId, SourceMachineKeyPurpose keyPurpose, SourceMachineKeyAlgorithm algorithm, string publicKey);

    /// <summary>
    /// Retrieves every active key for a device, newest first. More than one active operational key
    /// is legal during a rotation window, so this is deliberately a list.
    /// </summary>
    Task<List<SourceMachineKey>> GetActiveBySourceMachineIdAsync(int sourceMachineId);

    /// <summary>
    /// Resolves a presented public key to its active enrollment, or <see langword="null"/> if it is
    /// unknown or revoked.
    /// </summary>
    Task<SourceMachineKey?> GetActiveByPublicKeyAsync(string publicKey);

    /// <summary>
    /// Fetches an active key by its identifier, or <see langword="null"/> if it is unknown or
    /// revoked.
    /// </summary>
    Task<SourceMachineKey?> GetActiveByUuidAsync(Guid sourceMachineKeyUuid);


    /// <summary>
    /// Revokes a key -- a no-op, returning <see langword="null"/>, if it is already revoked or does
    /// not exist. The row is kept rather than deleted, so it stays possible to tell whether a key
    /// was trusted at a given time.
    /// </summary>
    Task<SourceMachineKey?> RevokeIfActiveAsync(Guid sourceMachineKeyUuid);
}
