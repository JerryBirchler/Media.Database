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
    Task<Words?> GetById(int id);

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
    Task<List<ViewWordFiles>> GetFilePagesByWordOrigin(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

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
    Task<List<ViewWordFiles>> GetFilePagesByWordFileId(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

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
    Task<List<ViewWordFiles>> GetFilePagesByFileIdOrigin(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

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
    Task<List<ViewWordFiles>> GetFilePagesByFileIdWord(string? word, WordOrigin? origin, Guid? fileId, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by file path, then origin.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="filePath">The file path to search for, or null to match any path.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetFilePagesByFilePathOrigin(string? word, WordOrigin? origin, Guid? fileId, string? filePath, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by file path, then word.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="filePath">The file path to search for, or null to match any path.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetFilePagesByFilePathWord(string? word, WordOrigin? origin, Guid? fileId, string? filePath, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a page of word/file rows ordered by word, then file path.
    /// </summary>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="filePath">The file path to search for, or null to match any path.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetFilePagesByWordFilePath(string? word, WordOrigin? origin, Guid? fileId, string? filePath, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a keyset-paged page of a single file's words, ordered by word. Equality-scoped by
    /// both <paramref name="fileId"/> and <paramref name="sourceMachineId"/> -- FileId alone would
    /// be enough to identify the file (it's globally unique), but SourceMachineId proves the file
    /// actually belongs to the caller before anything about it is returned.
    /// </summary>
    /// <param name="fileId">The file whose words to retrieve.</param>
    /// <param name="sourceMachineId">The authenticated device that must own <paramref name="fileId"/>.</param>
    /// <param name="afterWord">The keyset cursor: the last word seen on the previous page, or null for the first page.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetWordsByFileIdOrderedByWord(Guid fileId, int sourceMachineId, string? afterWord, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Same scoping as <see cref="GetWordsByFileIdOrderedByWord"/>, ordered by origin instead.
    /// </summary>
    /// <param name="fileId">The file whose words to retrieve.</param>
    /// <param name="sourceMachineId">The authenticated device that must own <paramref name="fileId"/>.</param>
    /// <param name="afterOrigin">The keyset cursor: the last origin seen on the previous page, or null for the first page.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetWordsByFileIdOrderedByOrigin(Guid fileId, int sourceMachineId, WordOrigin? afterOrigin, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves a keyset-paged page of a single file path's words, ordered by word.
    /// Equality-scoped by both <paramref name="filePath"/> and <paramref name="sourceMachineId"/>
    /// -- unlike a file id, a path is not globally unique across devices, so SourceMachineId is
    /// load-bearing for correctness here, not just authorization.
    /// </summary>
    /// <param name="filePath">The file path whose words to retrieve.</param>
    /// <param name="sourceMachineId">The authenticated device that must own <paramref name="filePath"/>.</param>
    /// <param name="afterWord">The keyset cursor: the last word seen on the previous page, or null for the first page.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    Task<List<ViewWordFiles>> GetWordsByFilePathOrderedByWord(string filePath, int sourceMachineId, string? afterWord, bool? isCurrent, bool? isProperName, int? limit = 10);

    /// <summary>
    /// Retrieves just the ordering-relevant identity (see <see cref="WordFileIdentifier"/>) of a
    /// page of word/file rows for the given <paramref name="orderBy"/> -- cheap enough to fetch a
    /// wide look-ahead range for cursor computation without hydrating every row's full content.
    /// Dispatches to whichever underlying query matches <paramref name="orderBy"/>, using whichever
    /// of <paramref name="next"/>'s <c>Word</c>/<c>FileId</c>/<c>OriginalFilePath</c> fields are
    /// actually relevant to that ordering. <paramref name="next"/> is the pointer to resume from --
    /// the identifier of the last row seen on the previous page (the same type this method itself
    /// returns), or null to start from the first page. Its fields always mean exactly what they
    /// say, never repurposed to hold a different kind of value for a different ordering.
    /// </summary>
    /// <param name="orderBy">Which composite sort order to page by.</param>
    /// <param name="next">The pointer to resume from, or null for the first page.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching rows' identifiers, in the same order the full page would be returned.</returns>
    Task<List<WordFileIdentifier>> GetFilePageIdentifiers(
        WordFilesOrderBy orderBy, WordFileIdentifier? next, WordOrigin? origin,
        bool? isCurrent, bool? isProperName, int limit);

    /// <summary>
    /// Hydrates full word/file rows for a set of (WordId, FileId) pairs, preferring Scylla's
    /// read-optimized word_files table and falling back to PostgreSQL per-pair when Scylla has no
    /// row yet (e.g. CDC hasn't caught up) or is unreachable. Lookups run with bounded parallelism
    /// rather than one at a time.
    /// </summary>
    /// <param name="ids">The (WordId, FileId) pairs to hydrate.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent lookups.</param>
    /// <returns>The hydrated rows, in no particular order -- callers that need a specific order must reorder themselves.</returns>
    Task<List<ViewWordFiles>> GetByIds(IEnumerable<(int WordId, Guid FileId)> ids, int maxDegreeOfParallelism);

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
    /// Retrieves every word currently linked to a file, with each link's own origin.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <returns>The file's currently-indexed word links.</returns>
    Task<List<WordFileLink>> GetWordsByFileId(Guid fileId);

    /// <summary>
    /// Removes a single word's link to a file, without deleting the shared word record --
    /// other files may still reference the same word.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <param name="wordId">The word's unique identifier.</param>
    Task DeleteWordFileLink(Guid fileId, int wordId);

}
