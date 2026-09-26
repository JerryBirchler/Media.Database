using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// The envelope serialized into the encrypted Scylla payload column.
///
/// An object rather than a bare array so the shape can gain a property without a version bump
/// being the only way to say anything new. The version itself is not in here -- it is the column
/// beside the blob, so there is one authority for the shape rather than two that can disagree.
/// </summary>
public record SearchListPayload
{
    /// <summary>The lines of an OR list, in the order the user arranged them.</summary>
    [JsonPropertyName("lines")]
    public required IReadOnlyList<SearchListLine> Lines { get; init; }
}
