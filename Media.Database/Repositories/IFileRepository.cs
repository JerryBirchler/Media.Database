using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides read, upsert, update, and delete operations for file records, keeping
/// PostgreSQL (system of record) and Scylla (read-optimized copy) in sync.
/// </summary>
public interface IFileRepository
{
    /// <summary>
    /// Retrieves a file by its unique identifier.
    /// </summary>
    /// <param name="id">The file's unique identifier.</param>
    /// <returns>The file, or null if not found.</returns>
    Task<Files?> GetById(Guid id);

    /// <summary>
    /// Retrieves the current version of a file for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path, or null to match any path.</param>
    /// <param name="limit">The maximum number of rows to consider.</param>
    /// <returns>The current file, or null if not found.</returns>
    Task<Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    /// <summary>
    /// Retrieves a page of current files for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path, or null to match any path.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching current files.</returns>
    Task<List<Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    /// <summary>
    /// Retrieves just the ordering key (Id, OriginalFilePath) for a page of current files, from
    /// PostgreSQL only -- cheap enough to fetch a wide look-ahead range for cursor computation
    /// without hydrating every row's full content.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path, or null to match any path.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching rows' identifiers, in the same order the full page would be returned.</returns>
    Task<List<(Guid Id, string OriginalFilePath)>> GetCurrentPageIdentifiersBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    /// <summary>
    /// Hydrates full file records for a set of ids, preferring Scylla's read-optimized copy and
    /// falling back to PostgreSQL per-id when Scylla has no row yet (e.g. CDC hasn't caught up) or
    /// is unreachable. Lookups run with bounded parallelism rather than one at a time.
    /// </summary>
    /// <param name="ids">The file ids to hydrate.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent lookups.</param>
    /// <returns>The hydrated files, in no particular order -- callers that need a specific order must reorder by id themselves.</returns>
    Task<List<Files>> GetByIds(IEnumerable<Guid> ids, int maxDegreeOfParallelism);

    /// <summary>
    /// Retrieves a page of historical (superseded) files for the given source machine and path.
    /// Identifies the page from PostgreSQL, then hydrates full rows preferring Scylla (with a
    /// PostgreSQL fallback per row), same split as <see cref="GetByIds"/> is used for elsewhere.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent hydration lookups.</param>
    /// <returns>The matching historical files.</returns>
    Task<List<Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5, int maxDegreeOfParallelism = 5);

    /// <summary>
    /// Inserts a new file record, or returns the existing one if it already exists unchanged.
    /// </summary>
    /// <param name="sourceMachineId">The identifier of the device that owns the file, resolved from the X-API-KEY.</param>
    /// <param name="request">The upload request describing the file.</param>
    /// <returns>The created or existing file, or null if the upsert did not return a row.</returns>
    Task<Files?> Upsert(int sourceMachineId, UploadFileRequest request);

    /// <summary>
    /// Updates an existing file's metadata and last-update timestamp.
    /// </summary>
    /// <param name="id">The unique identifier of the file to update.</param>
    /// <param name="request">The requested changes.</param>
    /// <returns>The update response, including the updated file and the derived word-level changes.</returns>
    Task<UpdateFileResponse> Update(Guid id, UpdateFileRequest request);

    /// <summary>
    /// Deletes a file by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file to delete.</param>
    /// <returns>The deleted file, or null if it did not exist.</returns>
    Task<Files?> Delete(Guid id);

    /// <summary>
    /// Deletes all historical files for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <returns>The deleted files.</returns>
    Task<List<Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath);

    /// <summary>
    /// Records that a thumbnail was generated for a file, by id. Called by Media.Worker's
    /// thumbnail-generation CDC handler once it has actually written a thumbnail to disk.
    /// </summary>
    /// <param name="id">The file's unique identifier.</param>
    /// <param name="generatedOn">When the thumbnail was generated.</param>
    Task SetThumbnailGeneratedOn(Guid id, DateTimeOffset generatedOn);

    /// <summary>
    /// Refreshes the current-files materialized view. Writes (Upsert/Update/Delete/DeleteHistoryBySourceMachineId)
    /// no longer refresh it inline -- this is called on an interval instead, only when something
    /// actually changed (see Media.Worker's FilesViewRefreshService/IFilesViewDirtyTracker).
    /// </summary>
    Task RefreshView();
}
