using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// A group's Data Encryption Key (DEK), wrapped under the group's own key (the KEK) -- MEDIA-35's
/// envelope encryption. <see cref="WrappedDek"/> is <c>Media.Common.Serialization.Encryptor</c>'s
/// own self-contained output (Base64(nonce||tag||ciphertext)), produced by wrapping the raw DEK's
/// Base64 form under the group's key -- never the raw DEK or the group's raw key itself.
/// </summary>
public record GroupEncryptionKey
{
    /// <summary>
    /// Gets the integer identifier for this key. Not <c>required</c>; see
    /// <see cref="Group.GroupId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int GroupEncryptionKeyId { get; init; }

    /// <summary>
    /// Gets the unique identifier for this key.
    /// </summary>
    public required Guid GroupEncryptionKeyUuid { get; init; }

    /// <summary>
    /// Gets the identifier of the shell this key belongs to. Not <c>required</c>; see
    /// <see cref="GroupEncryptionKeyId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int GroupShellId { get; init; }

    /// <summary>
    /// Gets which category of data this key's DEK protects.
    /// </summary>
    public required EncryptionDataCategory DataCategory { get; init; }

    /// <summary>
    /// Gets the wrapped (encrypted) DEK -- never the raw DEK or the group's raw key.
    /// </summary>
    public required string WrappedDek { get; init; }

    /// <summary>
    /// Gets a value indicating whether this key is the active one for its (GroupShellId,
    /// DataCategory) pair.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the timestamp when this key was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this key was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
