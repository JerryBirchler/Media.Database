using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides create/read operations for a group's wrapped Data Encryption Keys (MEDIA-35).
/// </summary>
public interface IGroupEncryptionKeyRepository
{
    /// <summary>
    /// Inserts a new active key for a (<paramref name="groupShellId"/>, <paramref name="dataCategory"/>)
    /// pair. <paramref name="wrappedDek"/> must already be wrapped -- this never sees the raw DEK
    /// or the group's raw key.
    /// </summary>
    Task<GroupEncryptionKey> CreateAsync(int groupShellId, EncryptionDataCategory dataCategory, string wrappedDek);

    /// <summary>
    /// Retrieves the single active key for a (<paramref name="groupShellId"/>,
    /// <paramref name="dataCategory"/>) pair, or <see langword="null"/> if none exists yet.
    /// </summary>
    Task<GroupEncryptionKey?> GetActiveAsync(int groupShellId, EncryptionDataCategory dataCategory);

    /// <summary>
    /// Deactivates every active key for a shell and reports how many were deactivated. Used by
    /// regeneration, which must clear the way before creating replacements.
    /// </summary>
    Task<int> DeactivateAllAsync(int groupShellId);
}
