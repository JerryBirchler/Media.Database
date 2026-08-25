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
    Task<Models.Files?> GetById(Guid id);

    /// <summary>
    /// Retrieves the current version of a file for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path, or null to match any path.</param>
    /// <param name="limit">The maximum number of rows to consider.</param>
    /// <returns>The current file, or null if not found.</returns>
    Task<Models.Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    /// <summary>
    /// Retrieves a page of current files for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path, or null to match any path.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching current files.</returns>
    Task<List<Models.Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    /// <summary>
    /// Retrieves a page of historical (superseded) files for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching historical files.</returns>
    Task<List<Models.Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5);

    /// <summary>
    /// Inserts a new file record, or returns the existing one if it already exists unchanged.
    /// </summary>
    /// <param name="request">The upload request describing the file.</param>
    /// <returns>The created or existing file, or null if the upsert did not return a row.</returns>
    Task<Models.Files?> Upsert(UploadFileRequest request);

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
    Task<Models.Files?> Delete(Guid id);

    /// <summary>
    /// Deletes all historical files for the given source machine and path.
    /// </summary>
    /// <param name="sourceMachineId">The source machine identifier.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <returns>The deleted files.</returns>
    Task<List<Models.Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath);
}
