using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Selects which composite sort order a word-to-file association page is keyset-paginated by.
/// Member names spell out the full (primary, secondary, tertiary) sort sequence, matching the
/// identifier query each one dispatches to on <see cref="Repositories.IWordRepository.GetFilePageIdentifiers"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WordFilesOrderBy
{
    /// <summary>Word, then origin, then file.</summary>
    WordOriginFileId,

    /// <summary>Word, then file, then origin.</summary>
    WordFileIdOrigin,

    /// <summary>File, then word, then origin.</summary>
    FileIdWordOrigin,

    /// <summary>File, then origin, then word.</summary>
    FileIdOriginWord,

    /// <summary>File path, then origin.</summary>
    FilePathOrigin,

    /// <summary>File path, then word.</summary>
    FilePathWord,

    /// <summary>Word, then file path.</summary>
    WordFilePath
}
