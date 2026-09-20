namespace Media.Database.Models;

/// <summary>
/// One person's encrypted access-UUID blob for one group (MEDIA-36) -- lets a verified person's
/// client fetch and locally decrypt its own <c>PersonSourceMachineUuid</c>/<c>GroupPersonUuid</c>
/// values (MEDIA-34) via the group's key, instead of those credentials ever being transmitted
/// over an API response. Scylla only, no Postgres counterpart -- this is the system of record for
/// its own rows, written directly, not hydrating anything CDC already carries. Encrypted
/// unconditionally, regardless of the group's Groups.IsEncrypted setting (MEDIA-11's PII policy
/// toggle doesn't gate this).
/// </summary>
public record GroupUuidOrchestration
{
    /// <summary>
    /// Gets the person this entry belongs to. Partition key -- a person's entries across every
    /// group they belong to are never fetchable by anyone but that person.
    /// </summary>
    public required Guid PersonUuid { get; init; }

    /// <summary>
    /// Gets the shell (see <see cref="GroupShell"/>) this entry's UUIDs belong to. Clustering key.
    /// </summary>
    public required int GroupShellId { get; init; }

    /// <summary>
    /// Gets the encrypted blob of this person's access UUIDs for this group -- ciphertext, in
    /// <c>Media.Common.Serialization.Encryptor</c>'s self-contained format, produced using the
    /// unwrapped <see cref="EncryptionDataCategory.UuidOrchestration"/> DEK. Never the raw UUIDs.
    /// </summary>
    public required string EncryptedUuidBlob { get; init; }

    /// <summary>
    /// Gets which <see cref="GroupEncryptionKey"/> (by its <see cref="GroupEncryptionKey.GroupEncryptionKeyUuid"/>)
    /// this entry's blob was encrypted under -- needed so a decrypting client knows which
    /// generation of the DEK to unwrap once rotation exists.
    /// </summary>
    public required Guid GroupEncryptionKeyUuid { get; init; }

    /// <summary>
    /// Gets the timestamp when this entry was last written.
    /// </summary>
    public required DateTimeOffset UpdatedOn { get; init; }
}
