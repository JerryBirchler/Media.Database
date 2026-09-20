using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Identifies which category of data a <see cref="GroupEncryptionKey"/>'s DEK protects -- separate
/// DEKs per category for blast-radius isolation (MEDIA-35). <see cref="UuidOrchestration"/> is
/// generated and used unconditionally regardless of a group's Groups.IsEncrypted setting;
/// <see cref="PiiMetadata"/> is not.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EncryptionDataCategory
{
    /// <summary>Protects CanBeEncrypted metadata fields (MEDIA-12), gated on Groups.IsEncrypted.</summary>
    PiiMetadata,

    /// <summary>Protects the UUID orchestration table (MEDIA-36). Always active, no opt-out.</summary>
    UuidOrchestration
}
