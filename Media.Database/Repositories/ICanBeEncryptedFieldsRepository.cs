using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// The CanBeEncryptedFields registry (MEDIA-12) -- Scylla-only record of every [CanBeEncrypted]
/// field ever shipped, keyed by (TypeName, MemberName), used to detect a field silently
/// disappearing from the code between releases (see Media.Api's reconciliation service).
/// </summary>
public interface ICanBeEncryptedFieldsRepository
{
    Task<List<CanBeEncryptedField>> GetAllAsync();

    /// <summary>Registers a newly-discovered field, stamped with the current release number.</summary>
    Task RegisterAsync(string typeName, string memberName, int releaseIntroduced);

    /// <summary>
    /// Records that a field was deliberately removed as of <paramref name="releaseRemoved"/> --
    /// only ever called explicitly by a developer acknowledging the removal, never by automatic
    /// reconciliation, or the whole safety mechanism this registry exists for is defeated.
    /// </summary>
    Task SetReleaseRemovedAsync(string typeName, string memberName, int releaseRemoved);
}
