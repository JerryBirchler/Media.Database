using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Identifies which part of a file's metadata a word was extracted from.
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
    FromLocation
}
