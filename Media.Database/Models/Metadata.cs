using Media.Common.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Descriptive metadata associated with a file, used both as request/response payload
/// and as the source of the words indexed for search. [CanBeEncrypted] at the object level
/// (MEDIA-12) -- every property here is an encryption candidate when the owning device/group's
/// resolved IsEncrypted is true. This is a storage-format concern only: the wire shape of this
/// class in API requests/responses is unaffected, wrapping happens solely in how it's serialized
/// into Scylla's Files.metadata column (not yet implemented -- see MEDIA-12's remaining scope).
/// </summary>
[CanBeEncrypted]
public class Metadata
{
    /// <summary>
    /// Gets or sets the set of keywords associated with the file.
    /// </summary>
    [JsonPropertyName("keyWords")]
    [FromHeader(Name = "keyWords")]
    public SortedSet<string>? KeyWords { get; set; } = null;

    /// <summary>
    /// Gets or sets the set of proper names associated with the file.
    /// </summary>
    [JsonPropertyName("names")]
    [FromHeader(Name = "names")]
    public SortedSet<string>? Names { get; set; } = null;

    /// <summary>
    /// Gets or sets the title of the file.
    /// </summary>
    [JsonPropertyName("title")]
    [FromHeader(Name = "title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the file.
    /// </summary>
    [JsonPropertyName("description")]
    [FromHeader(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the event associated with the file.
    /// </summary>
    [JsonPropertyName("event")]
    [FromHeader(Name = "event")]
    public string? Event { get; set; }

    /// <summary>
    /// Gets or sets the location associated with the file.
    /// </summary>
    [JsonPropertyName("location")]
    [FromHeader(Name = "location")]
    public string? Location { get; set; }
}
