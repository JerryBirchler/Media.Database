using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Selects which composite sort order a word-to-file association page is keyset-paginated by.
/// Member names spell out the full (primary, secondary, tertiary) sort sequence, matching the
/// query each one dispatches to on <see cref="Repositories.IWordRepository"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WordFilesOrderBy
{
    /// <summary>Word, then origin, then file. Backed by <c>IWordRepository.GetFilePagesByWordOrigin</c>.</summary>
    WordOriginFileId,

    /// <summary>Word, then file, then origin. Backed by <c>IWordRepository.GetFilePagesByWordFileId</c>.</summary>
    WordFileIdOrigin,

    /// <summary>File, then word, then origin. Backed by <c>IWordRepository.GetFilePagesByFileIdWord</c>.</summary>
    FileIdWordOrigin,

    /// <summary>File, then origin, then word. Backed by <c>IWordRepository.GetFilePagesByFileIdOrigin</c>.</summary>
    FileIdOriginWord,

    /// <summary>File path, then origin. Backed by <c>IWordRepository.GetFilePagesByFilePathOrigin</c>.</summary>
    FilePathOrigin,

    /// <summary>File path, then word. Backed by <c>IWordRepository.GetFilePagesByFilePathWord</c>.</summary>
    FilePathWord,

    /// <summary>Word, then file path. Backed by <c>IWordRepository.GetFilePagesByWordFilePath</c>.</summary>
    WordFilePath
}
