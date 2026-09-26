using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Identifies which part of a file a word or label came from.
///
/// Three kinds, and the kind decides the treatment. <see cref="Name"/> is asserted as a proper
/// noun because the field says so. <see cref="Keyword"/> and <see cref="FromFolder"/> are labels
/// somebody chose: kept whole, carrying no word type, never near the tagger. The From-prose
/// origins are split into words and tagged. Catalyst only ever sees prose.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WordOrigin
{
    /// <summary>
    /// The word came from the file's <see cref="Metadata.Names"/> collection.
    /// </summary>
    Name,

    /// <summary>
    /// The word came from the file's <see cref="Metadata.KeyWords"/> collection.
    /// </summary>
    Keyword,

    /// <summary>
    /// The word came from the file's <see cref="Metadata.Title"/>.
    /// </summary>
    FromTitle,

    /// <summary>
    /// The word came from the file's <see cref="Metadata.Description"/>.
    /// </summary>
    FromDescription,

    /// <summary>
    /// The word came from the file's <see cref="Metadata.Event"/>.
    /// </summary>
    FromEvent,

    /// <summary>
    /// The word came from the file's <see cref="Metadata.Location"/>.
    /// </summary>
    FromLocation,

    /// <summary>
    /// The entry is one segment of the file's folder path.
    ///
    /// Folders are vocabulary somebody already built -- Vacations, 2019, Kids School -- and
    /// usually not repeated in the metadata, so they are new search surface rather than a
    /// duplicate of the title.
    ///
    /// Treated exactly like a keyword: each segment kept whole, never split into words, and
    /// never given a word type. A folder name is a handle somebody chose, not prose.
    /// </summary>
    FromFolder
}
