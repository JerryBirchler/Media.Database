using Media.Database.Models;

namespace Media.Database.Services;

/// <summary>
/// Orchestrates a group's envelope encryption (MEDIA-11, MEDIA-35): generating the group's
/// default key, and creating/wrapping/unwrapping the per-category Data Encryption Keys that key
/// actually protects. Never persists or logs the raw group key or a raw DEK -- only wrapped DEKs
/// (<c>Media.Common.Serialization.Encryptor</c>'s own self-contained ciphertext) ever reach a
/// repository.
/// </summary>
public interface IGroupEncryptionKeyService
{
    /// <summary>
    /// Creates a new <see cref="GroupShell"/>, generates its default key, and creates the
    /// unconditional <see cref="EncryptionDataCategory.UuidOrchestration"/> DEK, wrapped under
    /// that key -- this category is never gated on a group's IsEncrypted setting. The raw key is
    /// returned so the caller can hand it back to the user exactly once; it is never persisted or
    /// logged anywhere in this call.
    /// </summary>
    Task<(GroupShell Shell, string RawKey)> CreateShellWithDefaultKeyAsync();

    /// <summary>
    /// Unwraps the active DEK for a (<paramref name="groupShellId"/>, <paramref name="dataCategory"/>)
    /// pair using <paramref name="rawKey"/>, or <see langword="null"/> if no key exists yet for
    /// that pair.
    /// </summary>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// <paramref name="rawKey"/> is wrong, or the stored value is corrupt.
    /// </exception>
    Task<byte[]?> UnwrapDekAsync(int groupShellId, EncryptionDataCategory dataCategory, string rawKey);

    /// <summary>
    /// Gets or lazily creates the <see cref="EncryptionDataCategory.PiiMetadata"/> DEK for
    /// <paramref name="groupShellId"/>, wrapped under <paramref name="rawKey"/>, unwraps it, and
    /// returns it re-encoded as a string suitable to pass as the <c>encryptionKey</c> argument
    /// wherever <c>Media.Common.Serialization.Encryptor</c>/<c>EncryptedFieldSerializer</c> expect
    /// one -- <c>[CanBeEncrypted]</c> fields are never encrypted with the raw group key directly,
    /// only with this DEK-derived value. Unlike
    /// <see cref="EncryptionDataCategory.UuidOrchestration"/>, this category's DEK does not exist
    /// until a group first turns its PII encryption on.
    /// </summary>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// <paramref name="rawKey"/> is wrong, or a stored value is corrupt.
    /// </exception>
    Task<string> ResolvePiiFieldEncryptionKeyAsync(int groupShellId, string rawKey);

    /// <summary>
    /// Reports whether this shell has ever encrypted PII -- true once its
    /// <see cref="EncryptionDataCategory.PiiMetadata"/> DEK exists. That DEK is created lazily, on
    /// the first encrypted write, so its existence is exactly the signal that regenerating the
    /// group key would strand real data: the old DEK cannot be unwrapped without the old key, so
    /// anything encrypted under it becomes permanently unreadable.
    /// </summary>
    Task<bool> HasEncryptedPiiAsync(int groupShellId);

    /// <summary>
    /// Replaces a shell's key material: deactivates every active key and issues a fresh raw key
    /// with a new UuidOrchestration DEK, returning the raw key for delivery. The
    /// <see cref="EncryptionDataCategory.PiiMetadata"/> DEK is deliberately not recreated here --
    /// it is created lazily on the next encrypted write, exactly as it is for a new shell.
    ///
    /// This is recovery, not rotation. Rotation re-wraps an existing DEK and preserves data;
    /// this cannot, because it exists for the case where the old key is gone.
    /// </summary>
    Task<string> RegenerateAsync(int groupShellId);
}
