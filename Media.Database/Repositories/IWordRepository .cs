using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Provides read, upsert, and delete operations for word records extracted from file metadata.
/// </summary>
public interface IWordRepository
{
    /// <summary>
    /// Retrieves a word by its unique identifier.
    /// </summary>
    /// <param name="id">The word's unique identifier.</param>
    /// <returns>The word, or null if not found.</returns>
    Task<Models.Words?> GetById(int id);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by word, then origin, then file.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<Models.ViewWordFiles>> GetFilePagesByWordOrigin(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by word, then file, then origin.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<Models.ViewWordFiles>> GetFilePagesByWordFileId(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by file, then origin, then word.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdOrigin(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by file, then word, then origin.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdWord(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Inserts a new word, or updates it if it already exists, and links it to the originating file.
    /// </summary>
    /// <param name="request">The upsert request describing the word.</param>
    Task Upsert(UpsertWordRequest request);

    /// <summary>
    /// Refreshes the materialized view backing the word/file page queries.
    /// </summary>
    Task RefreshView();

    /// <summary>
    /// Deletes a word by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the word to delete.</param>
    Task Delete(int id);

    /// <summary>
    /// Deletes all word/file links for the given file.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file.</param>
    Task DeleteFile(Guid fileId);

}
