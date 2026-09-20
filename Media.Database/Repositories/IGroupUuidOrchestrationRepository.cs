using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides write/read operations for a person's encrypted access-UUID blobs (MEDIA-36).
/// </summary>
public interface IGroupUuidOrchestrationRepository
{
    /// <summary>
    /// Writes (or overwrites) <paramref name="personUuid"/>'s orchestration entry for
    /// <paramref name="groupShellId"/>. <paramref name="encryptedUuidBlob"/> must already be
    /// encrypted -- this never sees the raw UUIDs it protects.
    /// </summary>
    Task UpsertAsync(Guid personUuid, int groupShellId, string encryptedUuidBlob, Guid groupEncryptionKeyUuid);

    /// <summary>
    /// Retrieves every orchestration entry for <paramref name="personUuid"/>, across every group
    /// they belong to.
    /// </summary>
    Task<List<GroupUuidOrchestration>> GetAllByPersonUuidAsync(Guid personUuid);
}
