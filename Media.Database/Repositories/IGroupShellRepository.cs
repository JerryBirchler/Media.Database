using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides create/read operations for <see cref="GroupShell"/>, the lightweight identity anchor
/// auto-created the moment a device registers with no existing group (MEDIA-37).
/// </summary>
public interface IGroupShellRepository
{
    /// <summary>Creates a new shell.</summary>
    Task<GroupShell> CreateAsync();

    /// <summary>Retrieves a shell by its identifier, or <see langword="null"/> if not found.</summary>
    Task<GroupShell?> GetByIdAsync(int groupShellId);
}
